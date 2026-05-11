import assert from "node:assert/strict";
import test from "node:test";
import {
  getDraftGenerationState,
  getEvidenceMatchingState,
  getJobAnalysisState,
  getProfileReadiness
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
      hasSavedApprovedEvidence: false
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
      hasSavedApprovedEvidence: false
    }),
    {
      canRun: false,
      message: "Save approved evidence before generating a draft."
    }
  );
});
