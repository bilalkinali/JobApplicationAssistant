import assert from "node:assert/strict";
import test from "node:test";
import {
  getDraftReadinessLabel,
  getDraftGenerationState,
  getEvidenceMatchingState,
  getGuidedNextAction,
  getJobAnalysisState,
  getPrepareApplicationPath,
  getProfileReadiness,
  getProviderReadinessTitle,
  getProviderRecoveryGuidance,
  getProviderSummary,
  getReadinessTone,
  isFakeProvider
} from "../dist-test/readiness.js";

test("profile readiness warns about missing contact setup without blocking evidence status", () => {
  const readiness = getProfileReadiness(
    {
      fullName: "Ada Lovelace",
      email: "",
      defaultLanguage: "English"
    },
    2
  );

  assert.equal(readiness.hasContactDetails, false);
  assert.equal(readiness.hasApprovedEvidence, true);
  assert.equal(
    readiness.contactMessage,
    "Profile setup is missing email. You can keep working, but later drafts and exports may be incomplete."
  );
  assert.equal(readiness.evidenceMessage, "2 approved profile facts are ready as evidence.");
});

test("profile readiness warns when approved evidence is missing", () => {
  const readiness = getProfileReadiness(
    {
      fullName: "Ada Lovelace",
      email: "ada@example.com",
      defaultLanguage: "English"
    },
    0
  );

  assert.equal(readiness.hasContactDetails, true);
  assert.equal(readiness.hasApprovedEvidence, false);
  assert.deepEqual(readiness.warnings, [
    "No approved profile facts yet. Add or approve at least one fact before evidence matching and draft generation."
  ]);
});

test("job analysis explains save and job posting blockers", () => {
  const unsavedState = getJobAnalysisState({
    selectedApplicationId: null,
    hasSavedJobPosting: false
  });

  assert.equal(unsavedState.canRun, false);
  assert.equal(unsavedState.message, "Save the application before running job analysis.");

  const missingPostingState = getJobAnalysisState({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: false
  });

  assert.equal(missingPostingState.canRun, false);
  assert.equal(missingPostingState.message, "Add and save job posting text before running job analysis.");
});

test("evidence matching explains missing-data blockers before provider concerns", () => {
  assert.deepEqual(
    getEvidenceMatchingState({
      selectedApplicationId: "application-1",
      hasJobSignals: false,
      approvedProfileFactCount: 1
    }),
    {
      canRun: false,
      message: "Run job analysis before matching evidence."
    }
  );

  assert.deepEqual(
    getEvidenceMatchingState({
      selectedApplicationId: "application-1",
      hasJobSignals: true,
      approvedProfileFactCount: 0
    }),
    {
      canRun: false,
      message: "Approve at least one profile fact before matching evidence."
    }
  );
});

test("draft generation explains job posting and approved evidence blockers", () => {
  assert.deepEqual(
    getDraftGenerationState({
      selectedApplicationId: "application-1",
      hasSavedJobPosting: false,
      hasSavedApprovedEvidence: false,
      unmatchedRequirementCount: 0,
      savedGapDecisionCount: 0
    }),
    {
      canRun: false,
      message: "Add and save job posting text before generating a draft."
    }
  );

  assert.deepEqual(
    getDraftGenerationState({
      selectedApplicationId: "application-1",
      hasSavedJobPosting: true,
      hasSavedApprovedEvidence: false,
      unmatchedRequirementCount: 0,
      savedGapDecisionCount: 0
    }),
    {
      canRun: false,
      message: "Save approved evidence before generating a draft."
    }
  );
});

test("draft generation waits for gap decisions", () => {
  assert.deepEqual(
    getDraftGenerationState({
      selectedApplicationId: "application-1",
      hasSavedJobPosting: true,
      hasSavedApprovedEvidence: true,
      unmatchedRequirementCount: 2,
      savedGapDecisionCount: 1
    }),
    {
      canRun: false,
      message: "Decide how to handle each unmatched requirement before generating a draft."
    }
  );
});

test("guided next action starts with saving the posting", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: false,
    preparationStatus: "NotStarted",
    approvedProfileFactCount: 1,
    savedApprovedEvidenceCount: 0,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "save-posting");
  assert.equal(action.title, "Paste and save the posting");
  assert.equal(action.buttonLabel, "Save posting");
});

test("guided next action prepares a saved posting before evidence review", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "NotStarted",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 0,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "prepare-application");
  assert.equal(action.title, "Prepare application");
  assert.equal(action.canRun, true);
});

test("prepare application path targets the backend orchestration endpoint", () => {
  assert.equal(
    getPrepareApplicationPath("application-1"),
    "/api/applications/application-1/prepare"
  );
});

test("guided next action shows preparation blockers plainly", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "NotStarted",
    approvedProfileFactCount: 0,
    savedApprovedEvidenceCount: 0,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "prepare-application");
  assert.equal(action.canRun, false);
  assert.equal(action.message, "Approve at least one profile fact before preparation can match evidence.");
});

test("guided next action moves from prepared evidence to review", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "PreparedForEvidenceReview",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 0,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "review-evidence");
  assert.equal(action.title, "Review evidence");
  assert.equal(action.buttonLabel, "Review matches");
});

test("guided next action stops at reviewed evidence before draft generation", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "PreparedForEvidenceReview",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 1,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "evidence-ready");
  assert.equal(action.title, "Evidence reviewed");
  assert.equal(action.buttonLabel, "Review evidence");
});

test("guided next action stays at evidence review while gaps are unresolved", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "PreparedForEvidenceReview",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 1,
    unmatchedRequirementCount: 2,
    savedGapDecisionCount: 1,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "review-evidence");
  assert.equal(action.title, "Resolve evidence gaps");
  assert.equal(action.buttonLabel, "Review gaps");
});

test("guided next action allows draft generation after all gaps are resolved", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "PreparedForEvidenceReview",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 1,
    unmatchedRequirementCount: 2,
    savedGapDecisionCount: 2,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "evidence-ready");
  assert.equal(action.title, "Evidence reviewed");
});

test("guided next action points provider failures toward diagnostics", () => {
  const action = getGuidedNextAction({
    selectedApplicationId: "application-1",
    hasSavedJobPosting: true,
    preparationStatus: "FailedProviderUnavailable",
    approvedProfileFactCount: 2,
    savedApprovedEvidenceCount: 0,
    unmatchedRequirementCount: 0,
    savedGapDecisionCount: 0,
    hasGeneratedDraft: false
  });

  assert.equal(action.kind, "ai-readiness");
  assert.equal(action.buttonLabel, "Open AI settings");
});

test("fake provider readiness is labeled as deterministic demo test behavior", () => {
  const status = {
    provider: "Fake",
    model: "fake-deterministic",
    endpoint: null,
    isAvailable: true,
    message: "Fake provider is available."
  };

  assert.equal(isFakeProvider(status), true);
  assert.equal(getReadinessTone(status), "warning");
  assert.equal(getProviderReadinessTitle(status), "Fake AI mode");
  assert.match(getProviderSummary(status), /Deterministic demo\/test behavior is active/);
  assert.equal(getDraftReadinessLabel(status), "Draft generation will use deterministic demo/test AI.");
});

test("real provider readiness includes recovery guidance for unavailable ollama", () => {
  const status = {
    provider: "Ollama",
    model: "llama3.1:8b",
    endpoint: "http://127.0.0.1:11434",
    isAvailable: false,
    message: "Ollama endpoint is unavailable."
  };

  assert.equal(isFakeProvider(status), false);
  assert.equal(getReadinessTone(status), "error");
  assert.equal(getProviderReadinessTitle(status), "Real AI provider unavailable");
  assert.match(getProviderSummary(status), /Ollama provider is unavailable with model llama3\.1:8b at http:\/\/127\.0\.0\.1:11434/);
  assert.match(getProviderRecoveryGuidance(status), /confirm the configured provider and model are available/i);
  assert.match(getProviderRecoveryGuidance(status), /http:\/\/127\.0\.0\.1:11434/);
  assert.equal(
    getDraftReadinessLabel(status),
    "Ollama llama3.1:8b is unavailable. Run diagnostics before retrying draft generation."
  );
});
