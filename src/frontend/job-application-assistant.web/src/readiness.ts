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
};

export type ActionState = {
  canRun: boolean;
  message: string;
};

export type GuidedNextActionKind =
  | "save-posting"
  | "prepare-application"
  | "review-evidence"
  | "evidence-ready"
  | "ai-readiness"
  | "complete";

export type GuidedNextAction = ActionState & {
  kind: GuidedNextActionKind;
  title: string;
  buttonLabel: string;
  tone: "info" | "warning" | "success" | "error";
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

  return {
    canRun: true,
    message: "Generate or edit the current cover letter and short motivation."
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

  if (input.preparationStatus === "FailedProviderUnavailable") {
    return {
      kind: "ai-readiness",
      title: "Check AI readiness",
      buttonLabel: "Open AI settings",
      canRun: true,
      tone: "error",
      message: "The provider was unavailable during preparation. Run diagnostics, then retry preparation."
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
    return {
      kind: "evidence-ready",
      title: "Evidence reviewed",
      buttonLabel: "Review evidence",
      canRun: true,
      tone: "success",
      message: "Approved evidence is saved. Continue with draft generation below when you are ready."
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
