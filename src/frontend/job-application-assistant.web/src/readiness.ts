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
};

export type ActionState = {
  canRun: boolean;
  message: string;
};

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

  return {
    canRun: true,
    message: "Generate or edit the current cover letter and short motivation."
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
