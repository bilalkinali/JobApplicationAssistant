export type ExportDraft = {
  coverLetterText: string;
  claimAudit?: string;
  auditUpdatedAt: string | null;
  isClaimAuditStale: boolean;
};

export type ExportApplication = {
  generatedDraft: ExportDraft | null;
};

export type CoverLetterExportState = {
  canExport: boolean;
  canCopy: boolean;
  reason: string | null;
};

export type CoverLetterExportOptions = {
  clipboardAvailable?: boolean;
  currentCoverLetterText?: string;
  hasUnsavedChanges?: boolean;
};

export function getCoverLetterExportState(
  application: ExportApplication | null | undefined,
  options: CoverLetterExportOptions = {}
): CoverLetterExportState {
  const coverLetterText = getCoverLetterText(application, options.currentCoverLetterText);
  if (!application?.generatedDraft) {
    return {
      canExport: false,
      canCopy: false,
      reason: "Generate a draft before exporting the cover letter."
    };
  }

  if (!coverLetterText.trim()) {
    return {
      canExport: false,
      canCopy: false,
      reason: "Cover letter text is required before export."
    };
  }

  if (options.hasUnsavedChanges) {
    return {
      canExport: false,
      canCopy: Boolean(options.clipboardAvailable),
      reason: "Save draft edits before downloading TXT or DOCX."
    };
  }

  return {
    canExport: true,
    canCopy: Boolean(options.clipboardAvailable),
    reason: null
  };
}

export function getAuditExportWarning(application: ExportApplication | null | undefined): string | null {
  const draft = application?.generatedDraft;
  if (!draft) {
    return null;
  }

  if (draft.isClaimAuditStale) {
    return "Claim audit is stale. You can export, but re-run audit before sending if you want the latest trust check.";
  }

  if (!draft.auditUpdatedAt || draft.claimAudit === "{}") {
    return "Claim audit has not been run. You can export, but this draft has not been checked against approved evidence yet.";
  }

  return null;
}

export function getCoverLetterText(
  application: ExportApplication | null | undefined,
  currentCoverLetterText?: string
): string {
  return currentCoverLetterText ?? application?.generatedDraft?.coverLetterText ?? "";
}
