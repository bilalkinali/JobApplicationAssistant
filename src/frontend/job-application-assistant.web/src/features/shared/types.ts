export type ProfileForm = {
  fullName: string;
  email: string;
  phone: string;
  location: string;
  linkedInUrl: string;
  gitHubUrl: string;
  portfolioUrl: string;
  defaultLanguage: string;
  danishTone: string;
  englishTone: string;
};

export type ProfileFactForm = {
  type: string;
  title: string;
  summary: string;
  status: string;
  factItems: string;
  technologies: string;
  allowedClaims: string;
  forbiddenClaims: string;
};

export type ProfileFact = ProfileFactForm & {
  id: string;
  createdAt: string;
  updatedAt: string;
  sourceDocumentIds: string;
  originalImportedSnapshot: string | null;
  manuallyEdited: boolean;
};

export type ImportedDraftFactReviewQueue = {
  importSessionId: string;
  fileName: string;
  draftFactCount: number;
  groups: ImportedDraftFactReviewGroup[];
};

export type ImportedDraftFactReviewGroup = {
  key: string;
  label: string;
  draftFactCount: number;
  facts: ImportedDraftFactReviewItem[];
};

export type ImportedDraftFactReviewItem = {
  profileFact: ProfileFact;
  sourceContext: string;
  hasDuplicateIndicators: boolean;
  duplicateIndicators: ImportedDraftFactDuplicateIndicator[];
};

export type ImportedDraftFactDuplicateIndicator = {
  scope: "ImportBatch" | "ExistingProfileFact" | string;
  profileFactId: string;
  profileFactTitle: string;
  reason: string;
};

export type AssistedProfileImportResponse = {
  importSessionId: string;
  fileName: string;
  importedFactCount: number;
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
  reviewUrl: string;
};

export type ImportedDraftFactDecisionResponse = {
  profileFact: ProfileFact;
  reviewQueue: ImportedDraftFactReviewQueue;
};

export type ImportedDraftFactBulkDecisionResponse = {
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
};

export type ImportedDraftFactMergeResponse = ImportedDraftFactDecisionResponse;

export type ImportedDraftFactSplitResponse = {
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
};

export type ApplicationForm = {
  companyName: string;
  roleTitle: string;
  applicationUrl: string;
  deadline: string;
  status: string;
  jobPostingText: string;
  detectedLanguage: string;
  selectedLanguage: string;
};

export type ApplicationSession = ApplicationForm & {
  id: string;
  jobSignals: string;
  evidenceMatches: string;
  unmatchedRequirements: string;
  candidateFitBrief: string;
  approvedEvidence: string;
  gapDecisions: string;
  customFacts: string;
  applicationStrategy: string;
  lastPreparedAt: string | null;
  preparationStatus: string;
  generatedDraft: GeneratedDraft | null;
  hasGeneratedDraft: boolean;
  auditReadiness: string;
  createdAt: string;
  updatedAt: string;
};

export type PrepareApplicationResult = {
  application: ApplicationSession;
  message: string;
};

export type GeneratedDraft = {
  id: string;
  jobApplicationId: string;
  coverLetterText: string;
  shortMotivationText: string;
  claimAudit: string;
  draftQualityCheck: string;
  generatedAt: string;
  lastEditedAt: string | null;
  auditUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string;
  isClaimAuditStale: boolean;
};

export type GeneratedDraftForm = {
  coverLetterText: string;
  shortMotivationText: string;
};

export type View = "home" | "profile" | "applications" | "settings";

export type JobSignalsDocument = {
  provider: string;
  extractedAt: string;
  requiredSkills: string[];
  preferredSkills: string[];
  responsibilities: string[];
  signals: JobSignal[];
};

export type JobSignal = {
  id: string;
  label: string;
  category: string;
  keywords: string[];
};

export type EvidenceMatch = {
  id: string;
  signalId: string;
  signal: string;
  category: string;
  profileFactId: string;
  profileFactTitle: string;
  summary: string;
  matchedTerms: string[];
  quality?: string | null;
  reason?: string | null;
};

export type CustomFact = {
  id: string;
  unmatchedRequirementId: string;
  title: string;
  summary: string;
  technologies?: string[];
  allowedClaims?: string[];
  status: "PendingConfirmation" | "Approved" | "Rejected" | string;
  createdAt: string;
  reviewedAt: string | null;
};

export type CustomFactDraft = {
  title: string;
  summary: string;
  technologies: string;
  allowedClaims: string;
};

export type UnmatchedRequirement = {
  id: string;
  signalId: string;
  requirement: string;
  category: string;
  recommendation: string;
};

export type GapDecisionValue = "Ignore" | "MentionAsLearningInterest" | "CoveredByCustomFact";

export type GapDecision = {
  unmatchedRequirementId: string;
  decision: GapDecisionValue;
  customFactId?: string;
};

export type ClaimAudit = {
  claims: ClaimAuditClaim[];
};

export type ClaimAuditClaim = {
  id: string;
  text: string;
  status: "Supported" | "Unsupported" | "NeedsReview" | string;
  evidenceIds: string[];
};

export type DraftQualityCheck = {
  status: "Passed" | "NeedsRevision" | string;
  issues: DraftQualityIssue[];
  copiedSevenWordPhraseCount: number;
  copiedPhraseThreshold: number;
};

export type DraftQualityIssue = {
  code: string;
  severity: "NeedsRevision" | string;
  message: string;
};

export type AiProviderStatus = {
  provider: string;
  model: string;
  endpoint: string | null;
  isAvailable: boolean;
  message: string;
};

export type AiDiagnostics = AiProviderStatus & {
  checks: AiDiagnosticCheck[];
};

export type AiDiagnosticCheck = {
  name: string;
  status: string;
  message: string;
};

export type InlineFeedback = {
  tone: "success" | "error";
  title: string;
  message: string;
  details?: string[];
};
