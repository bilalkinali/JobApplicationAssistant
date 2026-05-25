export type ExportDraft = {
  coverLetterText: string;
  claimAudit?: string;
  auditUpdatedAt: string | null;
  isClaimAuditStale: boolean;
  draftQualityCheck?: string;
};

export type ExportApplication = {
  generatedDraft: ExportDraft | null;
};

export type CoverLetterExportState = {
  canExport: boolean;
  canCopy: boolean;
  reason: string | null;
};

export type AuditExportNotice = {
  tone: "warning" | "neutral";
  message: string;
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

  if (draftNeedsRevision(application.generatedDraft.draftQualityCheck)) {
    return {
      canExport: false,
      canCopy: false,
      reason: "Resolve draft quality issues before exporting. Regenerate the draft, or edit and save it so the quality check passes."
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
  return getAuditExportNotice(application)?.message ?? null;
}

export function getAuditExportNotice(application: ExportApplication | null | undefined): AuditExportNotice | null {
  const draft = application?.generatedDraft;
  if (!draft) {
    return null;
  }

  if (draftNeedsRevision(draft.draftQualityCheck)) {
    return {
      tone: "warning",
      message: "Draft quality still blocks export. Supported claim counts only mean evidence support, not that the draft is ready to send."
    };
  }

  if (draft.isClaimAuditStale) {
    return {
      tone: "warning",
      message: "Claim audit is stale. Copy/export stays available, but refresh claim audit before sending if you want the latest trust check."
    };
  }

  if (!draft.auditUpdatedAt || draft.claimAudit === "{}") {
    return {
      tone: "neutral",
      message: "Claim audit has not been run. Copy/export stays available, but this draft has not been checked against approved evidence yet."
    };
  }

  return null;
}

export function getCoverLetterText(
  application: ExportApplication | null | undefined,
  currentCoverLetterText?: string
): string {
  return currentCoverLetterText ?? application?.generatedDraft?.coverLetterText ?? "";
}

function draftNeedsRevision(draftQualityCheck: string | undefined): boolean {
  if (!draftQualityCheck) {
    return false;
  }

  try {
    const parsed = JSON.parse(draftQualityCheck);
    return parsed?.status === "NeedsRevision";
  } catch {
    return false;
  }
}
