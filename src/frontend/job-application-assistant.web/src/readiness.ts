export type ProfileReadinessInput = {
  fullName?: string | null;
  email?: string | null;
  defaultLanguage?: string | null;
};

export type ProfileReadiness = {
  hasContactDetails: boolean;
  hasApprovedEvidence: boolean;
  contactMessage: string;
  evidenceMessage: string;
  warnings: string[];
};

export type DraftGenerationInput = {
  selectedApplicationId: string | null;
  hasSavedJobPosting: boolean;
  hasSavedApprovedEvidence: boolean;
  unmatchedRequirementCount: number;
  savedGapDecisionCount: number;
  hasGeneratedDraft: boolean;
  aiStatus?: AiProviderReadinessInput | null;
};

export type ActionState = {
  canRun: boolean;
  message: string;
};

export type GuidedNextActionKind =
  | "save-posting"
  | "prepare-application"
  | "review-evidence"
  | "generate-draft"
  | "refresh-audit"
  | "copy-export"
  | "ai-readiness"
  | "complete";

export type GuidedNextAction = ActionState & {
  kind: GuidedNextActionKind;
  title: string;
  buttonLabel: string;
  tone: "info" | "warning" | "success" | "error";
};

export type AiProviderReadinessInput = {
  provider: string;
  model: string;
  endpoint: string | null;
  isAvailable: boolean;
  message: string;
};

export function getPrepareApplicationPath(applicationId: string): string {
  return `/api/applications/${applicationId}/prepare`;
}

export function getProfileReadiness(
  profile: ProfileReadinessInput,
  approvedProfileFactCount: number
): ProfileReadiness {
  const missingContactFields = [
    hasText(profile.fullName) ? null : "full name",
    hasText(profile.email) ? null : "email",
    hasText(profile.defaultLanguage) ? null : "default language"
  ].filter((field): field is string => Boolean(field));
  const hasContactDetails = missingContactFields.length === 0;
  const hasApprovedEvidence = approvedProfileFactCount > 0;
  const evidenceCount =
    approvedProfileFactCount === 1
      ? "1 approved profile fact is ready as evidence."
      : `${approvedProfileFactCount} approved profile facts are ready as evidence.`;
  const contactMessage = hasContactDetails
    ? `${profile.fullName} has contact details ready for later drafts and exports.`
    : `Profile setup is missing ${joinList(missingContactFields)}. You can keep working, but later drafts and exports may be incomplete.`;
  const evidenceMessage = hasApprovedEvidence
    ? evidenceCount
    : "No approved profile facts yet. Add or approve at least one fact before evidence matching and draft generation.";

  return {
    hasContactDetails,
    hasApprovedEvidence,
    contactMessage,
    evidenceMessage,
    warnings: [hasContactDetails ? null : contactMessage, hasApprovedEvidence ? null : evidenceMessage].filter(
      (warning): warning is string => Boolean(warning)
    )
  };
}

export function getJobAnalysisState(input: {
  selectedApplicationId: string | null;
  hasSavedJobPosting: boolean;
}): ActionState {
  if (!input.selectedApplicationId) {
    return {
      canRun: false,
      message: "Save the application before running job analysis."
    };
  }

  if (!input.hasSavedJobPosting) {
    return {
      canRun: false,
      message: "Add and save job posting text before running job analysis."
    };
  }

  return {
    canRun: true,
    message: "Extract signals from the saved job posting."
  };
}

export function getEvidenceMatchingState(input: {
  selectedApplicationId: string | null;
  hasJobSignals: boolean;
  approvedProfileFactCount: number;
}): ActionState {
  if (!input.selectedApplicationId) {
    return {
      canRun: false,
      message: "Save the application before matching evidence."
    };
  }

  if (!input.hasJobSignals) {
    return {
      canRun: false,
      message: "Run job analysis before matching evidence."
    };
  }

  if (input.approvedProfileFactCount === 0) {
    return {
      canRun: false,
      message: "Approve at least one profile fact before matching evidence."
    };
  }

  return {
    canRun: true,
    message: "Match analyzed signals against approved profile facts."
  };
}

export function getDraftGenerationState(input: DraftGenerationInput): ActionState {
  if (!input.selectedApplicationId) {
    return {
      canRun: false,
      message: "Save the application before generating a draft."
    };
  }

  if (!input.hasSavedJobPosting) {
    return {
      canRun: false,
      message: "Add and save job posting text before generating a draft."
    };
  }

  if (!input.hasSavedApprovedEvidence) {
    return {
      canRun: false,
      message: "Save approved evidence before generating a draft."
    };
  }

  if (input.savedGapDecisionCount < input.unmatchedRequirementCount) {
    return {
      canRun: false,
      message: "Decide how to handle each unmatched requirement before generating a draft."
    };
  }

  if (isUnavailableRealProvider(input.aiStatus)) {
    return {
      canRun: false,
      message: getWorkflowReadinessRecoveryMessage(input.aiStatus, "draft generation")
    };
  }

  return {
    canRun: true,
    message: "Generate the current cover letter and short motivation, then audit claims against approved evidence."
  };
}

export function getGuidedNextAction(input: {
  selectedApplicationId: string | null;
  hasSavedJobPosting: boolean;
  preparationStatus: string;
  approvedProfileFactCount: number;
  savedApprovedEvidenceCount: number;
  unmatchedRequirementCount: number;
  savedGapDecisionCount: number;
  hasGeneratedDraft: boolean;
  auditReadiness: string;
  hasUnsavedDraftEdits?: boolean;
  canCopyOrExport?: boolean;
  aiStatus?: AiProviderReadinessInput | null;
}): GuidedNextAction {
  if (!input.selectedApplicationId || !input.hasSavedJobPosting) {
    return {
      kind: "save-posting",
      title: "Paste and save the posting",
      buttonLabel: input.selectedApplicationId ? "Save posting" : "Create application",
      canRun: true,
      tone: "warning",
      message: "Add the job posting text, then save the application before AI preparation can start."
    };
  }

  if (
    isUnavailableRealProvider(input.aiStatus) &&
    input.preparationStatus !== "PreparedForEvidenceReview" &&
    input.preparationStatus !== "FailedInvalidProviderOutput"
  ) {
    return {
      kind: "ai-readiness",
      title: "Check AI readiness",
      buttonLabel: "Open AI settings",
      canRun: true,
      tone: "error",
      message: getWorkflowReadinessRecoveryMessage(input.aiStatus, "preparation")
    };
  }

  if (input.preparationStatus === "FailedProviderUnavailable") {
    return {
      kind: "ai-readiness",
      title: "Check AI readiness",
      buttonLabel: "Open AI settings",
      canRun: true,
      tone: "error",
      message: getWorkflowReadinessRecoveryMessage(input.aiStatus, "preparation")
    };
  }

  if (input.preparationStatus === "FailedInvalidProviderOutput") {
    return {
      kind: "ai-readiness",
      title: "Review AI diagnostics",
      buttonLabel: "Open AI settings",
      canRun: true,
      tone: "error",
      message: "The provider returned output the app could not safely use. Check diagnostics, then retry preparation."
    };
  }

  if (input.preparationStatus === "PartiallyPreparedAnalysisOnly") {
    return {
      kind: "prepare-application",
      title: "Retry preparation",
      buttonLabel: "Retry preparation",
      canRun: input.approvedProfileFactCount > 0,
      tone: "warning",
      message:
        input.approvedProfileFactCount > 0
          ? "Job analysis is available, but evidence matching needs to finish before evidence review is ready."
          : "Approve at least one profile fact before retrying evidence matching."
    };
  }

  if (input.preparationStatus !== "PreparedForEvidenceReview") {
    return {
      kind: "prepare-application",
      title: "Prepare application",
      buttonLabel: "Prepare application",
      canRun: input.approvedProfileFactCount > 0,
      tone: input.approvedProfileFactCount > 0 ? "info" : "warning",
      message:
        input.approvedProfileFactCount > 0
          ? "Analyze the saved posting and match it against approved profile facts."
          : "Approve at least one profile fact before preparation can match evidence."
    };
  }

  if (input.savedApprovedEvidenceCount === 0) {
    return {
      kind: "review-evidence",
      title: "Review evidence",
      buttonLabel: "Review matches",
      canRun: true,
      tone: "info",
      message: "Preparation is complete. Approve the matches that may support generated application text."
    };
  }

  if (input.savedGapDecisionCount < input.unmatchedRequirementCount) {
    return {
      kind: "review-evidence",
      title: "Resolve evidence gaps",
      buttonLabel: "Review gaps",
      canRun: true,
      tone: "info",
      message: "Approved evidence is saved. Decide how to handle each unmatched requirement before moving on."
    };
  }

  if (!input.hasGeneratedDraft) {
    if (isUnavailableRealProvider(input.aiStatus)) {
      return {
        kind: "ai-readiness",
        title: "Check AI readiness",
        buttonLabel: "Open AI settings",
        canRun: true,
        tone: "error",
        message: getWorkflowReadinessRecoveryMessage(input.aiStatus, "draft generation")
      };
    }

    return {
      kind: "generate-draft",
      title: "Generate and audit draft",
      buttonLabel: "Generate draft",
      canRun: true,
      tone: "success",
      message: "Approved evidence and gap decisions are saved. Generate the draft and claim audit in one step."
    };
  }

  if (input.hasUnsavedDraftEdits || input.auditReadiness === "Stale" || input.auditReadiness === "Missing") {
    if (isUnavailableRealProvider(input.aiStatus)) {
      return {
        kind: "ai-readiness",
        title: "Check AI readiness",
        buttonLabel: "Open AI settings",
        canRun: true,
        tone: "error",
        message: getWorkflowReadinessRecoveryMessage(input.aiStatus, "claim audit refresh")
      };
    }

    return {
      kind: "refresh-audit",
      title: "Refresh claim audit",
      buttonLabel: "Refresh claim audit",
      canRun: true,
      tone: input.auditReadiness === "Current" ? "info" : "warning",
      message: input.hasUnsavedDraftEdits
        ? "Draft edits need to be saved and checked against approved evidence before final use."
        : "The current draft needs a fresh claim audit before copy or export is the final guided action."
    };
  }

  if (input.auditReadiness === "Current") {
    return {
      kind: "copy-export",
      title: "Copy or export",
      buttonLabel: input.canCopyOrExport ? "Review copy/export" : "Review export options",
      canRun: true,
      tone: "success",
      message: "The claim audit is current. Use the copy and TXT/DOCX export options when ready."
    };
  }

  return {
    kind: "complete",
    title: "Draft ready",
    buttonLabel: "Review draft",
    canRun: true,
    tone: "success",
    message: "The application has a generated draft. Review edits, audit claims, and export when ready."
  };
}

export function getProviderSummary(status: AiProviderReadinessInput): string {
  const availability = status.isAvailable ? "available" : "unavailable";
  const endpoint = status.endpoint ? ` at ${status.endpoint}` : "";
  const deterministic = isFakeProvider(status) ? " Deterministic demo/test behavior is active." : "";

  return `${status.provider} provider is ${availability} with model ${status.model}${endpoint}. ${status.message}${deterministic}`;
}

export function getAvailabilityLabel(status: AiProviderReadinessInput): string {
  return status.isAvailable ? "Available" : "Unavailable";
}

export function getReadinessTone(status: AiProviderReadinessInput): "info" | "warning" | "error" {
  if (isFakeProvider(status)) {
    return "warning";
  }

  return status.isAvailable ? "info" : "error";
}

export function getProviderReadinessTitle(status: AiProviderReadinessInput): string {
  if (isFakeProvider(status)) {
    return "Fake AI mode";
  }

  return status.isAvailable ? "Real AI provider ready" : "Real AI provider unavailable";
}

export function getProviderRecoveryGuidance(status: AiProviderReadinessInput): string {
  const endpoint = status.endpoint ? ` Confirm ${status.endpoint} is reachable.` : "";
  return `Run diagnostics from AI settings and confirm the configured provider and model are available before retrying AI workflow actions.${endpoint}`;
}

export function getWorkflowReadinessRecoveryMessage(
  status: AiProviderReadinessInput | null | undefined,
  actionName: string
): string {
  if (!status) {
    return `AI readiness needs attention. Run diagnostics before retrying ${actionName}.`;
  }

  if (isFakeProvider(status)) {
    return "Fake AI mode is ready and uses deterministic demo/test output.";
  }

  if (status.isAvailable) {
    return `${status.provider} ${status.model} is ready for ${actionName}.`;
  }

  return `${status.provider} ${status.model} is unavailable. Run diagnostics before retrying ${actionName}.`;
}

export function getDraftReadinessLabel(status: AiProviderReadinessInput): string {
  if (isFakeProvider(status)) {
    return "Draft generation will use deterministic demo/test AI.";
  }

  if (status.isAvailable) {
    return `${status.provider} ${status.model} is ready for draft generation.`;
  }

  return `${status.provider} ${status.model} is unavailable. Run diagnostics before retrying draft generation.`;
}

export function isFakeProvider(status: AiProviderReadinessInput): boolean {
  return status.provider.toLowerCase() === "fake";
}

function isUnavailableRealProvider(status: AiProviderReadinessInput | null | undefined): status is AiProviderReadinessInput {
  return Boolean(status && !isFakeProvider(status) && !status.isAvailable);
}

function hasText(value: string | null | undefined): boolean {
  return Boolean(value?.trim());
}

function joinList(values: string[]): string {
  if (values.length <= 1) {
    return values[0] ?? "";
  }

  if (values.length === 2) {
    return `${values[0]} and ${values[1]}`;
  }

  return `${values.slice(0, -1).join(", ")}, and ${values[values.length - 1]}`;
}
