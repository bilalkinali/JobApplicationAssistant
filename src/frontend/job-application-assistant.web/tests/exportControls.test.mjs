import assert from "node:assert/strict";
import test from "node:test";
import {
  getAuditExportWarning,
  getCoverLetterExportState,
  getCoverLetterText
} from "../dist-test/exportControls.js";

const baseApplication = {
  generatedDraft: {
    coverLetterText: "Current cover letter.",
    claimAudit: '{"claims":[]}',
    auditUpdatedAt: "2026-05-11T10:00:00Z",
    isClaimAuditStale: false
  }
};

test("cover letter export is blocked when no generated draft exists", () => {
  const state = getCoverLetterExportState(null);

  assert.equal(state.canExport, false);
  assert.equal(state.reason, "Generate a draft before exporting the cover letter.");
});

test("cover letter export is blocked when cover letter text is empty", () => {
  const state = getCoverLetterExportState({
    generatedDraft: {
      ...baseApplication.generatedDraft,
      coverLetterText: "   "
    }
  });

  assert.equal(state.canExport, false);
  assert.equal(state.reason, "Cover letter text is required before export.");
});

test("stale and missing audit warnings do not block export", () => {
  const staleApplication = {
    generatedDraft: {
      ...baseApplication.generatedDraft,
      isClaimAuditStale: true
    }
  };
  const missingApplication = {
    generatedDraft: {
      ...baseApplication.generatedDraft,
      auditUpdatedAt: null
    }
  };

  assert.equal(getCoverLetterExportState(staleApplication).canExport, true);
  assert.equal(getAuditExportWarning(staleApplication), "Claim audit is stale. Copy/export stays available, but refresh claim audit before sending if you want the latest trust check.");
  assert.equal(getCoverLetterExportState(missingApplication).canExport, true);
  assert.equal(getAuditExportWarning(missingApplication), "Claim audit has not been run. Copy/export stays available, but this draft has not been checked against approved evidence yet.");
});

test("copy uses the current cover letter text only when draft text exists and clipboard is available", () => {
  assert.equal(getCoverLetterText(baseApplication, "Visible edited cover letter."), "Visible edited cover letter.");
  assert.equal(getCoverLetterExportState(baseApplication, { clipboardAvailable: true }).canCopy, true);
  assert.equal(getCoverLetterExportState(baseApplication, { clipboardAvailable: false }).canCopy, false);
});

test("downloads are blocked until visible draft edits are saved", () => {
  const state = getCoverLetterExportState(baseApplication, {
    clipboardAvailable: true,
    currentCoverLetterText: "Visible edited cover letter.",
    hasUnsavedChanges: true
  });

  assert.equal(state.canExport, false);
  assert.equal(state.canCopy, true);
  assert.equal(state.reason, "Save draft edits before downloading TXT or DOCX.");
});

test("copy and downloads are blocked when draft quality needs revision", () => {
  const state = getCoverLetterExportState({
    generatedDraft: {
      ...baseApplication.generatedDraft,
      draftQualityCheck: '{"status":"NeedsRevision","issues":[]}'
    }
  }, { clipboardAvailable: true });

  assert.equal(state.canExport, false);
  assert.equal(state.canCopy, false);
  assert.equal(state.reason, "Resolve draft quality issues before exporting. Regenerate the draft, or edit and save it so the quality check passes.");
});

test("claim audit is missing when the audit payload is still empty", () => {
  const state = {
    generatedDraft: {
      ...baseApplication.generatedDraft,
      claimAudit: "{}"
    }
  };

  assert.equal(getAuditExportWarning(state), "Claim audit has not been run. Copy/export stays available, but this draft has not been checked against approved evidence yet.");
});
