import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import {
  getAuditExportNotice,
  getCoverLetterExportState,
  getCoverLetterText
} from "./exportControls";
import {
  ErrorPresentation,
  formatError,
  plainError,
  technicalDetails
} from "./errorPresentation";
import {
  getAvailabilityLabel,
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
} from "./readiness";
import "./styles.css";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5108";
const emptyProfile: ProfileForm = {
  fullName: "",
  email: "",
  phone: "",
  location: "",
  linkedInUrl: "",
  gitHubUrl: "",
  portfolioUrl: "",
  defaultLanguage: "English",
  danishTone: "",
  englishTone: ""
};
const emptyApplication: ApplicationForm = {
  companyName: "",
  roleTitle: "",
  applicationUrl: "",
  deadline: "",
  status: "Draft",
  jobPostingText: "",
  detectedLanguage: "",
  selectedLanguage: ""
};
const emptyProfileFact: ProfileFactForm = {
  type: "Experience",
  title: "",
  summary: "",
  status: "Draft",
  factItems: "[]",
  technologies: "[]",
  allowedClaims: "[]",
  forbiddenClaims: "[]"
};
const applicationStatuses = ["Draft", "PostingCaptured", "ReadyForReview", "PreparedForEvidenceReview", "Applied", "Archived"];
const auditReadinessOptions = ["All", "Current", "Stale", "Missing", "NotApplicable"];
const profileFactStatuses = ["Draft", "Approved", "Archived", "Rejected"];

type ProfileForm = {
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

type ProfileFactForm = {
  type: string;
  title: string;
  summary: string;
  status: string;
  factItems: string;
  technologies: string;
  allowedClaims: string;
  forbiddenClaims: string;
};

type ProfileFact = ProfileFactForm & {
  id: string;
  createdAt: string;
  updatedAt: string;
  sourceDocumentIds: string;
  originalImportedSnapshot: string | null;
  manuallyEdited: boolean;
};

type ImportedDraftFactReviewQueue = {
  importSessionId: string;
  fileName: string;
  draftFactCount: number;
  groups: ImportedDraftFactReviewGroup[];
};

type ImportedDraftFactReviewGroup = {
  key: string;
  label: string;
  draftFactCount: number;
  facts: ImportedDraftFactReviewItem[];
};

type ImportedDraftFactReviewItem = {
  profileFact: ProfileFact;
  sourceContext: string;
  hasDuplicateIndicators: boolean;
  duplicateIndicators: ImportedDraftFactDuplicateIndicator[];
};

type ImportedDraftFactDuplicateIndicator = {
  scope: "ImportBatch" | "ExistingProfileFact" | string;
  profileFactId: string;
  profileFactTitle: string;
  reason: string;
};

type AssistedProfileImportResponse = {
  importSessionId: string;
  fileName: string;
  importedFactCount: number;
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
  reviewUrl: string;
};

type ImportedDraftFactDecisionResponse = {
  profileFact: ProfileFact;
  reviewQueue: ImportedDraftFactReviewQueue;
};

type ImportedDraftFactBulkDecisionResponse = {
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
};

type ImportedDraftFactMergeResponse = ImportedDraftFactDecisionResponse;

type ImportedDraftFactSplitResponse = {
  profileFacts: ProfileFact[];
  reviewQueue: ImportedDraftFactReviewQueue;
};

type ApplicationForm = {
  companyName: string;
  roleTitle: string;
  applicationUrl: string;
  deadline: string;
  status: string;
  jobPostingText: string;
  detectedLanguage: string;
  selectedLanguage: string;
};

type ApplicationSession = ApplicationForm & {
  id: string;
  jobSignals: string;
  evidenceMatches: string;
  unmatchedRequirements: string;
  approvedEvidence: string;
  gapDecisions: string;
  customFacts: string;
  lastPreparedAt: string | null;
  preparationStatus: string;
  generatedDraft: GeneratedDraft | null;
  hasGeneratedDraft: boolean;
  auditReadiness: string;
  createdAt: string;
  updatedAt: string;
};

type PrepareApplicationResult = {
  application: ApplicationSession;
  message: string;
};

type GeneratedDraft = {
  id: string;
  jobApplicationId: string;
  coverLetterText: string;
  shortMotivationText: string;
  claimAudit: string;
  generatedAt: string;
  lastEditedAt: string | null;
  auditUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string;
  isClaimAuditStale: boolean;
};

type GeneratedDraftForm = {
  coverLetterText: string;
  shortMotivationText: string;
};

type View = "home" | "profile" | "applications" | "settings";

type JobSignalsDocument = {
  provider: string;
  extractedAt: string;
  requiredSkills: string[];
  preferredSkills: string[];
  responsibilities: string[];
  signals: JobSignal[];
};

type JobSignal = {
  id: string;
  label: string;
  category: string;
  keywords: string[];
};

type EvidenceMatch = {
  id: string;
  signalId: string;
  signal: string;
  category: string;
  profileFactId: string;
  profileFactTitle: string;
  summary: string;
  matchedTerms: string[];
};

type CustomFact = {
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

type CustomFactDraft = {
  title: string;
  summary: string;
  technologies: string;
  allowedClaims: string;
};

type UnmatchedRequirement = {
  id: string;
  signalId: string;
  requirement: string;
  category: string;
  recommendation: string;
};

type GapDecisionValue = "Ignore" | "MentionAsLearningInterest" | "CoveredByCustomFact";

type GapDecision = {
  unmatchedRequirementId: string;
  decision: GapDecisionValue;
  customFactId?: string;
};

type ClaimAudit = {
  claims: ClaimAuditClaim[];
};

type ClaimAuditClaim = {
  id: string;
  text: string;
  status: "Supported" | "Unsupported" | "NeedsReview" | string;
  evidenceIds: string[];
};

type AiProviderStatus = {
  provider: string;
  model: string;
  endpoint: string | null;
  isAvailable: boolean;
  message: string;
};

type AiDiagnostics = AiProviderStatus & {
  checks: AiDiagnosticCheck[];
};

type AiDiagnosticCheck = {
  name: string;
  status: string;
  message: string;
};

type InlineFeedback = {
  tone: "success" | "error";
  title: string;
  message: string;
  details?: string[];
};

function App() {
  const [view, setView] = useState<View>("home");
  const [profile, setProfile] = useState<ProfileForm>(emptyProfile);
  const [profileFacts, setProfileFacts] = useState<ProfileFact[]>([]);
  const [importReviewQueues, setImportReviewQueues] = useState<ImportedDraftFactReviewQueue[]>([]);
  const [selectedImportedDraftFactIds, setSelectedImportedDraftFactIds] = useState<string[]>([]);
  const [splitImportedDraftFacts, setSplitImportedDraftFacts] = useState("");
  const [profileImportFile, setProfileImportFile] = useState<File | null>(null);
  const [profileImportBusy, setProfileImportBusy] = useState(false);
  const [pendingImportReviewFocusId, setPendingImportReviewFocusId] = useState<string | null>(null);
  const [profileFactForm, setProfileFactForm] = useState<ProfileFactForm>(emptyProfileFact);
  const [selectedProfileFactId, setSelectedProfileFactId] = useState<string | null>(null);
  const [applications, setApplications] = useState<ApplicationSession[]>([]);
  const [applicationForm, setApplicationForm] = useState<ApplicationForm>(emptyApplication);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);
  const [applicationSearch, setApplicationSearch] = useState("");
  const [applicationStatusFilter, setApplicationStatusFilter] = useState("All");
  const [applicationReadinessFilter, setApplicationReadinessFilter] = useState("All");
  const [includeArchivedApplications, setIncludeArchivedApplications] = useState(false);
  const [approvedEvidenceDraft, setApprovedEvidenceDraft] = useState<EvidenceMatch[]>([]);
  const [gapDecisionsDraft, setGapDecisionsDraft] = useState<GapDecision[]>([]);
  const [customFactDrafts, setCustomFactDrafts] = useState<Record<string, CustomFactDraft>>({});
  const [generatedDraftForm, setGeneratedDraftForm] = useState<GeneratedDraftForm>({
    coverLetterText: "",
    shortMotivationText: ""
  });
  const [workflowBusy, setWorkflowBusy] = useState<string | null>(null);
  const [exportBusy, setExportBusy] = useState<"txt" | "docx" | null>(null);
  const [exportFeedback, setExportFeedback] = useState<InlineFeedback | null>(null);
  const [profileImportFeedback, setProfileImportFeedback] = useState<InlineFeedback | null>(null);
  const [isClipboardAvailable, setIsClipboardAvailable] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiProviderStatus | null>(null);
  const [aiDiagnostics, setAiDiagnostics] = useState<AiDiagnostics | null>(null);
  const [aiDiagnosticsError, setAiDiagnosticsError] = useState<ErrorPresentation | null>(null);
  const [aiDiagnosticsBusy, setAiDiagnosticsBusy] = useState(false);
  const [aiDiagnosticsLastRanAt, setAiDiagnosticsLastRanAt] = useState<string | null>(null);
  const [error, setError] = useState<ErrorPresentation | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const evidenceReviewRef = useRef<HTMLDivElement | null>(null);
  const importReviewRef = useRef<HTMLDivElement | null>(null);
  const profileImportFileRef = useRef<HTMLInputElement | null>(null);
  const draftReviewRef = useRef<HTMLElement | null>(null);
  const exportPanelRef = useRef<HTMLElement | null>(null);

  const selectedApplication = useMemo(
    () => applications.find((application) => application.id === selectedApplicationId),
    [applications, selectedApplicationId]
  );
  const approvedProfileFacts = useMemo(
    () => profileFacts.filter((fact) => fact.status === "Approved"),
    [profileFacts]
  );
  const selectedImportedDraftFact = useMemo(
    () =>
      profileFacts.find(
        (fact) =>
          fact.id === selectedProfileFactId &&
          fact.status === "Draft" &&
          Boolean(importSessionIdFromProfileFact(fact))
      ),
    [profileFacts, selectedProfileFactId]
  );
  const selectedImportReviewQueue = useMemo(
    () =>
      selectedImportedDraftFact
        ? importReviewQueues.find((queue) => queue.importSessionId === importSessionIdFromProfileFact(selectedImportedDraftFact))
        : null,
    [importReviewQueues, selectedImportedDraftFact]
  );
  const filteredApplications = useMemo(() => {
    const search = applicationSearch.trim().toLowerCase();

    return applications.filter((application) => {
      const matchesStatus = applicationStatusFilter === "All" || application.status === applicationStatusFilter;
      const matchesReadiness =
        applicationReadinessFilter === "All" || application.auditReadiness === applicationReadinessFilter;
      const matchesSearch =
        !search ||
        application.companyName.toLowerCase().includes(search) ||
        application.roleTitle.toLowerCase().includes(search);

      return matchesStatus && matchesReadiness && matchesSearch;
    });
  }, [applications, applicationReadinessFilter, applicationSearch, applicationStatusFilter]);
  const jobSignals = useMemo(
    () => parseJobSignals(selectedApplication?.jobSignals),
    [selectedApplication?.jobSignals]
  );
  const evidenceMatches = useMemo(
    () => parseJsonArray<EvidenceMatch>(selectedApplication?.evidenceMatches),
    [selectedApplication?.evidenceMatches]
  );
  const unmatchedRequirements = useMemo(
    () => parseJsonArray<UnmatchedRequirement>(selectedApplication?.unmatchedRequirements),
    [selectedApplication?.unmatchedRequirements]
  );
  const savedApprovedEvidence = useMemo(
    () => parseJsonArray<EvidenceMatch>(selectedApplication?.approvedEvidence),
    [selectedApplication?.approvedEvidence]
  );
  const savedGapDecisions = useMemo(
    () => parseJsonArray<GapDecision>(selectedApplication?.gapDecisions),
    [selectedApplication?.gapDecisions]
  );
  const customFacts = useMemo(
    () => parseJsonArray<CustomFact>(selectedApplication?.customFacts),
    [selectedApplication?.customFacts]
  );
  const savedApprovedCustomFactEvidenceCount = useMemo(
    () => countApprovedCustomFactEvidence(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const savedApprovedCustomFactEvidence = useMemo(
    () => approvedCustomFactEvidenceItems(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const currentGapDecisionCount = useMemo(
    () => countCurrentGapDecisions(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const claimAudit = useMemo(
    () => parseClaimAudit(selectedApplication?.generatedDraft?.claimAudit),
    [selectedApplication?.generatedDraft?.claimAudit]
  );
  const hasSavedJobPosting = Boolean(selectedApplication?.jobPostingText.trim());
  const hasSavedApprovedEvidence = savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount > 0;
  const hasGeneratedDraft = Boolean(selectedApplication?.generatedDraft);
  const auditSummary = useMemo(() => summarizeClaimAudit(claimAudit), [claimAudit]);
  const hasUnsavedDraftEdits =
    Boolean(selectedApplication?.generatedDraft) &&
    (generatedDraftForm.coverLetterText !== selectedApplication?.generatedDraft?.coverLetterText ||
      generatedDraftForm.shortMotivationText !== selectedApplication?.generatedDraft?.shortMotivationText);
  const coverLetterExportState = useMemo(
    () =>
      getCoverLetterExportState(selectedApplication, {
        clipboardAvailable: isClipboardAvailable,
        currentCoverLetterText: generatedDraftForm.coverLetterText,
        hasUnsavedChanges: hasUnsavedDraftEdits
      }),
    [generatedDraftForm.coverLetterText, hasUnsavedDraftEdits, isClipboardAvailable, selectedApplication]
  );
  const auditExportNotice = useMemo(
    () => getAuditExportNotice(selectedApplication),
    [selectedApplication]
  );
  const effectiveAuditReadiness = hasUnsavedDraftEdits
    ? "Stale"
    : selectedApplication?.auditReadiness ?? auditReadinessForDraft(selectedApplication?.generatedDraft ?? null);
  const isRealProviderUnavailable = Boolean(aiStatus && !isFakeProvider(aiStatus) && !aiStatus.isAvailable);
  const canRefreshClaimAudit =
    Boolean(selectedApplication?.generatedDraft) &&
    effectiveAuditReadiness !== "Current" &&
    !isRealProviderUnavailable;
  const effectiveAuditExportNotice = hasUnsavedDraftEdits
    ? {
        tone: "warning" as const,
        message: "Claim audit is stale because the draft has unsaved edits. Refresh claim audit to save and re-check the edited text."
      }
    : auditExportNotice;
  const profileReadiness = useMemo(
    () => getProfileReadiness(profile, approvedProfileFacts.length),
    [approvedProfileFacts.length, profile]
  );
  const jobAnalysisState = useMemo(
    () =>
      getJobAnalysisState({
        selectedApplicationId,
        hasSavedJobPosting
      }),
    [hasSavedJobPosting, selectedApplicationId]
  );
  const evidenceMatchingState = useMemo(
    () =>
      getEvidenceMatchingState({
        selectedApplicationId,
        hasJobSignals: jobSignals.signals.length > 0,
        approvedProfileFactCount: approvedProfileFacts.length
      }),
    [approvedProfileFacts.length, jobSignals.signals.length, selectedApplicationId]
  );
  const draftGenerationState = useMemo(
    () =>
      getDraftGenerationState({
        selectedApplicationId,
        hasSavedJobPosting,
        hasSavedApprovedEvidence,
        unmatchedRequirementCount: unmatchedRequirements.length,
        savedGapDecisionCount: currentGapDecisionCount,
        hasGeneratedDraft,
        aiStatus
      }),
    [aiStatus, currentGapDecisionCount, hasGeneratedDraft, hasSavedApprovedEvidence, hasSavedJobPosting, selectedApplicationId, unmatchedRequirements.length]
  );
  const guidedNextAction = useMemo(
    () =>
      getGuidedNextAction({
        selectedApplicationId,
        hasSavedJobPosting,
        preparationStatus: selectedApplication?.preparationStatus ?? "NotStarted",
        approvedProfileFactCount: approvedProfileFacts.length,
        savedApprovedEvidenceCount: savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount,
        unmatchedRequirementCount: unmatchedRequirements.length,
        savedGapDecisionCount: currentGapDecisionCount,
        hasGeneratedDraft,
        auditReadiness: effectiveAuditReadiness,
        hasUnsavedDraftEdits,
        canCopyOrExport: coverLetterExportState.canCopy || coverLetterExportState.canExport,
        aiStatus
      }),
    [
      aiStatus,
      approvedProfileFacts.length,
      coverLetterExportState.canCopy,
      coverLetterExportState.canExport,
      currentGapDecisionCount,
      effectiveAuditReadiness,
      hasGeneratedDraft,
      hasSavedJobPosting,
      hasUnsavedDraftEdits,
      savedApprovedEvidence.length,
      savedApprovedCustomFactEvidenceCount,
      selectedApplication?.preparationStatus,
      selectedApplicationId,
      unmatchedRequirements.length
    ]
  );
  const workflowBusyReason = workflowBusy ? "Wait for the current workflow action to finish." : null;
  const exportBusyReason = exportBusy ? "Wait for the current export action to finish." : null;

  useEffect(() => {
    void loadProfile();
    void loadProfileFacts();
    void loadAiStatus();
  }, []);

  useEffect(() => {
    setIsClipboardAvailable(Boolean(navigator.clipboard?.writeText));
  }, []);

  useEffect(() => {
    setApprovedEvidenceDraft(savedApprovedEvidence);
  }, [selectedApplicationId, savedApprovedEvidence]);

  useEffect(() => {
    setGapDecisionsDraft(savedGapDecisions);
  }, [selectedApplicationId, savedGapDecisions]);

  useEffect(() => {
    setCustomFactDrafts({});
  }, [selectedApplicationId]);

  useEffect(() => {
    setGeneratedDraftForm({
      coverLetterText: selectedApplication?.generatedDraft?.coverLetterText ?? "",
      shortMotivationText: selectedApplication?.generatedDraft?.shortMotivationText ?? ""
    });
    setExportFeedback(null);
  }, [selectedApplicationId, selectedApplication?.generatedDraft]);

  async function loadProfile() {
    try {
      const response = await apiGet<ProfileForm>("/api/profile");
      setProfile(toProfileForm(response));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadApplications() {
    try {
      const response = await apiGet<ApplicationSession[]>(
        `/api/applications?includeArchived=${includeArchivedApplications}`
      );
      setApplications(response.map(toApplicationSession));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  useEffect(() => {
    void loadApplications();
  }, [includeArchivedApplications]);

  useEffect(() => {
    void loadImportedDraftFactReviewQueues();
  }, [profileFacts]);

  useEffect(() => {
    if (!pendingImportReviewFocusId || !importReviewQueues.some((queue) => queue.importSessionId === pendingImportReviewFocusId)) {
      return;
    }

    importReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
    setPendingImportReviewFocusId(null);
  }, [importReviewQueues, pendingImportReviewFocusId]);

  async function loadProfileFacts() {
    try {
      const response = await apiGet<ProfileFact[]>("/api/profile/facts");
      setProfileFacts(response.map(toProfileFact));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadImportedDraftFactReviewQueues() {
    const importSessionIds = Array.from(new Set(profileFacts.map(importSessionIdFromProfileFact).filter((id): id is string => Boolean(id))));
    if (importSessionIds.length === 0) {
      setImportReviewQueues([]);
      return;
    }

    try {
      const queues = await Promise.all(
        importSessionIds.map((importSessionId) => apiGet<ImportedDraftFactReviewQueue>(`/api/profile/imports/${importSessionId}/draft-facts`))
      );
      setImportReviewQueues(queues.filter((queue) => queue.draftFactCount > 0));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadAiStatus() {
    try {
      const response = await apiGet<AiProviderStatus>("/api/ai/status");
      setAiStatus(response);
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const saved = await apiSend<ProfileForm>("/api/profile", "PUT", profile);
      setProfile(toProfileForm(saved));
      setNotice("Profile saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveApplication(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const payload = toApplicationPayload(applicationForm);
      const path = selectedApplicationId
        ? `/api/applications/${selectedApplicationId}`
        : "/api/applications";
      const method = selectedApplicationId ? "PUT" : "POST";
      const saved = await apiSend<ApplicationSession>(path, method, payload);

      setApplicationForm(toApplicationForm(saved));
      setSelectedApplicationId(saved.id);
      await loadApplications();
      setNotice("Application saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveProfileFact(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const path = selectedProfileFactId
        ? `/api/profile/facts/${selectedProfileFactId}`
        : "/api/profile/facts";
      const method = selectedProfileFactId ? "PUT" : "POST";
      const saved = await apiSend<ProfileFact>(path, method, profileFactForm);

      setProfileFactForm(toProfileFactForm(saved));
      setSelectedProfileFactId(saved.id);
      await loadProfileFacts();
      setNotice("Profile fact saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function deleteProfileFact() {
    if (!selectedProfileFactId) {
      return;
    }

    if (!window.confirm("Delete this profile fact? This permanently removes it from your evidence library.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await apiDelete(`/api/profile/facts/${selectedProfileFactId}`);
      startNewProfileFact();
      await loadProfileFacts();
      setNotice("Profile fact deleted.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function importProfilePdfCv(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profileImportFile) {
      setError(plainError("Validation blocker", "Choose a PDF CV before starting assisted import."));
      return;
    }

    setError(null);
    setNotice(null);
    setProfileImportFeedback(null);
    setProfileImportBusy(true);

    try {
      const form = new FormData();
      form.append("file", profileImportFile);
      const response = await apiSendForm<AssistedProfileImportResponse>("/api/profile/imports/pdf-cv", form);
      setImportReviewQueues((queues) => [
        response.reviewQueue,
        ...queues.filter((queue) => queue.importSessionId !== response.importSessionId)
      ]);
      setPendingImportReviewFocusId(response.importSessionId);
      setSelectedImportedDraftFactIds([]);
      if (response.profileFacts[0]) {
        setSelectedProfileFactId(response.profileFacts[0].id);
        setProfileFactForm(toProfileFactForm(response.profileFacts[0]));
        setSplitImportedDraftFacts(splitDraftTemplate(response.profileFacts[0]));
      }
      await loadProfileFacts();
      setProfileImportFile(null);
      if (profileImportFileRef.current) {
        profileImportFileRef.current.value = "";
      }
      setProfileImportFeedback({
        tone: "success",
        title: "Imported facts ready for review",
        message: `${response.importedFactCount} draft profile fact${response.importedFactCount === 1 ? "" : "s"} from ${response.fileName} are grouped below. Approve the strong ones to make them available for application evidence matching.`
      });
    } catch (apiError) {
      setError(formatError(apiError));
      setProfileImportFeedback({
        tone: "error",
        title: "Assisted import did not change your profile",
        message: "Review the diagnostics above, then retry the PDF import when the provider is ready."
      });
    } finally {
      setProfileImportBusy(false);
    }
  }

  async function reviewImportedDraftFact(
    fact: ProfileFact,
    decision: "approve" | "archive" | "reject",
    importSessionId = importSessionIdFromProfileFact(fact),
    profileFact?: ProfileFactForm
  ) {
    if (!importSessionId) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactDecisionResponse>(
        `/api/profile/imports/${importSessionId}/draft-facts/${fact.id}/decision`,
        "POST",
        {
          decision,
          profileFact
        }
      );
      setImportReviewQueues((queues) =>
        response.reviewQueue.draftFactCount > 0
          ? queues.map((queue) => (queue.importSessionId === response.reviewQueue.importSessionId ? response.reviewQueue : queue))
          : queues.filter((queue) => queue.importSessionId !== response.reviewQueue.importSessionId)
      );
      setProfileFactForm(toProfileFactForm(response.profileFact));
      setSelectedProfileFactId(response.profileFact.id);
      await loadProfileFacts();
      setNotice(
        decision === "approve"
          ? "Imported fact approved and available for application evidence matching."
          : `Imported fact ${importDecisionPastTense(decision)}.`
      );
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function bulkReviewImportedDraftFacts(queue: ImportedDraftFactReviewQueue, decision: "approve" | "archive") {
    const profileFactIds = selectedIdsForQueue(queue);
    if (profileFactIds.length === 0) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactBulkDecisionResponse>(
        `/api/profile/imports/${queue.importSessionId}/draft-facts/bulk-decision`,
        "POST",
        { decision, profileFactIds }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => !profileFactIds.includes(id)));
      await loadProfileFacts();
      setNotice(
        decision === "approve"
          ? `${profileFactIds.length} imported facts approved and available for application evidence matching.`
          : `${profileFactIds.length} imported facts ${importDecisionPastTense(decision)}.`
      );
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function mergeImportedDraftFacts(queue: ImportedDraftFactReviewQueue) {
    const profileFactIds = selectedIdsForQueue(queue);
    if (profileFactIds.length < 2) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactMergeResponse>(
        `/api/profile/imports/${queue.importSessionId}/draft-facts/merge`,
        "POST",
        { profileFactIds }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => !profileFactIds.includes(id)));
      setSelectedProfileFactId(response.profileFact.id);
      setProfileFactForm(toProfileFactForm(response.profileFact));
      await loadProfileFacts();
      setNotice("Imported facts merged.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function splitImportedDraftFact() {
    if (!selectedImportedDraftFact) {
      return;
    }

    const importSessionId = importSessionIdFromProfileFact(selectedImportedDraftFact);
    if (!importSessionId) {
      return;
    }

    let profileFacts: ProfileFactForm[];
    try {
      profileFacts = JSON.parse(splitImportedDraftFacts) as ProfileFactForm[];
    } catch {
      setError(plainError("Validation blocker", "Split facts must be a JSON array of profile fact drafts."));
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactSplitResponse>(
        `/api/profile/imports/${importSessionId}/draft-facts/${selectedImportedDraftFact.id}/split`,
        "POST",
        { profileFacts }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => id !== selectedImportedDraftFact.id));
      if (response.profileFacts[0]) {
        setSelectedProfileFactId(response.profileFacts[0].id);
        setProfileFactForm(toProfileFactForm(response.profileFacts[0]));
      }
      setSplitImportedDraftFacts("");
      await loadProfileFacts();
      setNotice("Imported fact split.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  function updateImportReviewQueue(reviewQueue: ImportedDraftFactReviewQueue) {
    setImportReviewQueues((queues) =>
      reviewQueue.draftFactCount > 0
        ? queues.map((queue) => (queue.importSessionId === reviewQueue.importSessionId ? reviewQueue : queue))
        : queues.filter((queue) => queue.importSessionId !== reviewQueue.importSessionId)
    );
  }

  function toggleImportedDraftFactSelection(factId: string) {
    setSelectedImportedDraftFactIds((ids) =>
      ids.includes(factId) ? ids.filter((id) => id !== factId) : [...ids, factId]
    );
  }

  function selectedIdsForQueue(queue: ImportedDraftFactReviewQueue) {
    const queueIds = new Set(queue.groups.flatMap((group) => group.facts.map((item) => item.profileFact.id)));
    return selectedImportedDraftFactIds.filter((id) => queueIds.has(id));
  }

  async function deleteApplication() {
    if (!selectedApplicationId) {
      return;
    }

    if (!window.confirm("Delete this application session? This permanently removes its workflow state.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await apiDelete(`/api/applications/${selectedApplicationId}`);
      setApplicationForm(emptyApplication);
      setSelectedApplicationId(null);
      await loadApplications();
      setNotice("Application deleted.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function markApplicationStatus(status: "Applied" | "Archived") {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before changing its final status."));
      return;
    }

    if (status === "Archived" && !window.confirm("Archive this application session? It will move out of the active history unless archived sessions are included.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/status`,
        "PUT",
        { status }
      );
      replaceApplication(saved);
      await loadApplications();
      if (status === "Archived" && !includeArchivedApplications) {
        setSelectedApplicationId(null);
        setApplicationForm(emptyApplication);
      }
      setNotice(status === "Applied" ? "Application marked applied." : "Application archived.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function analyzeJob() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before running job analysis."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("analysis");

    try {
      const saved = await apiSend<ApplicationSession>(`/api/applications/${selectedApplicationId}/analyze-job`, "POST", null);
      replaceApplication(saved);
      setNotice("Job analysis updated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function matchEvidence() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before matching evidence."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("matching");

    try {
      const saved = await apiSend<ApplicationSession>(`/api/applications/${selectedApplicationId}/match-evidence`, "POST", null);
      replaceApplication(saved);
      setNotice("Evidence matching updated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function prepareApplication() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before preparing it."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("prepare");

    try {
      const result = await apiSend<PrepareApplicationResult>(
        getPrepareApplicationPath(selectedApplicationId),
        "POST",
        null
      );
      replaceApplication(result.application);
      setNotice(result.message);
    } catch (apiError) {
      setError(formatError(apiError));
      await refreshSelectedApplication(selectedApplicationId);
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function saveApprovedEvidence() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before reviewing evidence."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("review");

    try {
      await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/approved-evidence`,
        "PUT",
        { approvedEvidence: JSON.stringify(approvedEvidenceDraft) }
      );
      const reviewed = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/gap-decisions`,
        "PUT",
        { gapDecisions: JSON.stringify(gapDecisionsDraft) }
      );
      replaceApplication(reviewed);
      setNotice("Evidence review saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function createCustomFact(unmatchedRequirementId: string) {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before adding job-local facts."));
      return;
    }

    const draft = customFactDrafts[unmatchedRequirementId] ?? emptyCustomFactDraft();

    setError(null);
    setNotice(null);
    setWorkflowBusy(`custom-fact-${unmatchedRequirementId}`);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/custom-facts`,
        "POST",
        {
          unmatchedRequirementId,
          title: draft.title,
          summary: draft.summary,
          technologies: splitLines(draft.technologies),
          allowedClaims: splitLines(draft.allowedClaims)
        }
      );
      replaceApplication(saved);
      setCustomFactDrafts((current) => ({ ...current, [unmatchedRequirementId]: emptyCustomFactDraft() }));
      setNotice("Job-local custom fact added for review.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function updateCustomFactStatus(customFactId: string, status: "Approved" | "Rejected") {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before reviewing job-local facts."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy(`custom-fact-status-${customFactId}`);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/custom-facts/${customFactId}/status`,
        "PUT",
        { status }
      );
      replaceApplication(saved);
      setNotice(status === "Approved" ? "Job-local custom fact approved." : "Job-local custom fact rejected.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function generateDraft() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before generating a draft."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("draft");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/generate-draft`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice(draft.auditUpdatedAt ? "Draft generated and claim audit updated." : "Draft generated. Claim audit needs retry before final review.");
      scrollDraftReviewSoon();
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function saveGeneratedDraft(): Promise<GeneratedDraft | null> {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before editing a draft."));
      return null;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("draft-edit");

    try {
      const draft = await apiSend<GeneratedDraft>(
        `/api/applications/${selectedApplicationId}/generated-draft`,
        "PUT",
        generatedDraftForm
      );
      replaceGeneratedDraft(draft);
      setNotice("Draft edits saved.");
      return draft;
    } catch (apiError) {
      setError(formatError(apiError));
      return null;
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function auditClaims(): Promise<GeneratedDraft | null> {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before auditing a draft."));
      return null;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("audit");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/audit-claims`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice("Claim audit updated.");
      scrollDraftReviewSoon();
      return draft;
    } catch (apiError) {
      setError(formatError(apiError));
      return null;
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function refreshClaimAudit() {
    if (hasUnsavedDraftEdits) {
      const saved = await saveGeneratedDraft();
      if (!saved) {
        return;
      }
    }

    await auditClaims();
  }

  async function copyCoverLetter() {
    setError(null);
    setNotice(null);

    if (!coverLetterExportState.canCopy) {
      setExportFeedback({
        tone: "error",
        title: "Copy blocked",
        message: coverLetterExportState.reason ?? "Clipboard copy is not available in this browser."
      });
      return;
    }

    setExportFeedback(null);

    try {
      await navigator.clipboard.writeText(getCoverLetterText(selectedApplication, generatedDraftForm.coverLetterText));
      setExportFeedback({
        tone: "success",
        title: "Cover letter copied",
        message: "The current edited cover letter text is on the clipboard."
      });
    } catch (apiError) {
      setExportFeedback({
        tone: "error",
        title: "Clipboard copy failed",
        message: "Clipboard copy failed. You can still select and copy the cover letter manually.",
        details: technicalDetails(apiError)
      });
    }
  }

  async function downloadCoverLetter(format: "txt" | "docx") {
    setError(null);
    setNotice(null);

    if (!selectedApplicationId || !coverLetterExportState.canExport) {
      setExportFeedback({
        tone: "error",
        title: "Export blocked",
        message: coverLetterExportState.reason ?? "Select an application with a generated cover letter before exporting."
      });
      return;
    }

    setExportFeedback(null);
    setExportBusy(format);

    try {
      const response = await fetch(`${apiBaseUrl}/api/applications/${selectedApplicationId}/exports/cover-letter.${format}`);
      if (!response.ok) {
        throw await response.json();
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileNameFromContentDisposition(
        response.headers.get("content-disposition"),
        `cover-letter.${format}`
      );
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      setExportFeedback({
        tone: "success",
        title: `${format.toUpperCase()} cover letter downloaded`,
        message: "The download used the current saved cover letter text without regenerating or re-running audit."
      });
    } catch (apiError) {
      const presentation = formatError(apiError);
      setExportFeedback({
        tone: "error",
        title: presentation.title,
        message: presentation.message,
        details: presentation.details
      });
    } finally {
      setExportBusy(null);
    }
  }

  async function runAiDiagnostics() {
    setError(null);
    setNotice(null);
    setAiDiagnosticsError(null);
    setAiDiagnosticsBusy(true);

    try {
      const diagnostics = await apiSend<AiDiagnostics>("/api/ai/diagnostics", "POST", null);
      setAiDiagnostics(diagnostics);
      setAiDiagnosticsLastRanAt(new Date().toISOString());
      setAiStatus({
        provider: diagnostics.provider,
        model: diagnostics.model,
        endpoint: diagnostics.endpoint,
        isAvailable: diagnostics.isAvailable,
        message: diagnostics.message
      });
      setNotice("AI diagnostics updated.");
    } catch (apiError) {
      setAiDiagnosticsLastRanAt(new Date().toISOString());
      setAiDiagnosticsError(formatError(apiError));
    } finally {
      setAiDiagnosticsBusy(false);
    }
  }

  async function openApplication(application: ApplicationSession) {
    setSelectedApplicationId(application.id);
    setApplicationForm(toApplicationForm(application));
    setView("applications");
    setError(null);
    setNotice(null);

    try {
      const detailed = await apiGet<ApplicationSession>(`/api/applications/${application.id}`);
      replaceApplication(detailed);
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function refreshSelectedApplication(applicationId: string) {
    try {
      const detailed = await apiGet<ApplicationSession>(`/api/applications/${applicationId}`);
      replaceApplication(detailed);
    } catch {
      // Keep the original preparation error visible.
    }
  }

  function runGuidedNextAction() {
    switch (guidedNextAction.kind) {
      case "prepare-application":
        void prepareApplication();
        return;
      case "review-evidence":
        evidenceReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      case "generate-draft":
        void generateDraft();
        return;
      case "refresh-audit":
        void refreshClaimAudit();
        return;
      case "copy-export":
        exportPanelRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      case "ai-readiness":
        setView("settings");
        return;
      case "complete":
        draftReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      default:
        return;
    }
  }

  function scrollDraftReviewSoon() {
    window.setTimeout(() => draftReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" }), 0);
  }

  function replaceApplication(application: ApplicationSession) {
    const normalized = toApplicationSession(application);
    setApplications((current) => current.map((item) => (item.id === normalized.id ? normalized : item)));
    setSelectedApplicationId(normalized.id);
    setApplicationForm(toApplicationForm(normalized));
  }

  function replaceGeneratedDraft(draft: GeneratedDraft) {
    setApplications((current) =>
      current.map((application) =>
        application.id === draft.jobApplicationId
          ? toApplicationSession({
              ...application,
              generatedDraft: draft,
              hasGeneratedDraft: true,
              auditReadiness: auditReadinessForDraft(draft),
              updatedAt: draft.updatedAt
            })
          : application
      )
    );
    setGeneratedDraftForm({
      coverLetterText: draft.coverLetterText,
      shortMotivationText: draft.shortMotivationText
    });
  }

  function approveMatch(match: EvidenceMatch) {
    setApprovedEvidenceDraft((current) =>
      current.some((item) => item.id === match.id) ? current : [...current, match]
    );
  }

  function removeApprovedEvidence(matchId: string) {
    setApprovedEvidenceDraft((current) => current.filter((item) => item.id !== matchId));
  }

  function decideGap(unmatchedRequirementId: string, decision: GapDecisionValue, customFactId?: string) {
    setGapDecisionsDraft((current) => [
      ...current.filter((item) => item.unmatchedRequirementId !== unmatchedRequirementId),
      { unmatchedRequirementId, decision, customFactId }
    ]);
  }

  function updateCustomFactDraft(unmatchedRequirementId: string, changes: Partial<CustomFactDraft>) {
    setCustomFactDrafts((current) => ({
      ...current,
      [unmatchedRequirementId]: {
        ...emptyCustomFactDraft(),
        ...current[unmatchedRequirementId],
        ...changes
      }
    }));
  }

  function startNewApplication() {
    setSelectedApplicationId(null);
    setApplicationForm(emptyApplication);
    setView("applications");
    setError(null);
    setNotice(null);
  }

  function openProfileFact(fact: ProfileFact) {
    setSelectedProfileFactId(fact.id);
    setProfileFactForm(toProfileFactForm(fact));
    setSplitImportedDraftFacts(importSessionIdFromProfileFact(fact) ? splitDraftTemplate(fact) : "");
    setError(null);
    setNotice(null);
  }

  function startNewProfileFact() {
    setSelectedProfileFactId(null);
    setProfileFactForm(emptyProfileFact);
    setSplitImportedDraftFacts("");
    setError(null);
    setNotice(null);
  }

  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="brand">
          <span className="brand-mark">JA</span>
          <div>
            <p className="eyebrow">V1 workspace</p>
            <h1>Job Application Assistant</h1>
          </div>
        </div>

        <nav className="navigation">
          {(["home", "profile", "applications", "settings"] satisfies View[]).map((item) => (
            <button
              className={view === item ? "active" : ""}
              key={item}
              type="button"
              onClick={() => setView(item)}
            >
              {titleCase(item)}
            </button>
          ))}
        </nav>
      </aside>

      <section className="workspace" aria-labelledby="workspace-title">
        <header className="workspace-header">
          <div>
            <p className="eyebrow">Workbench</p>
            <h2 id="workspace-title">{pageTitle(view)}</h2>
          </div>
          <span
            className={`status-pill ${aiStatus?.isAvailable === false ? "unavailable" : ""} ${
              aiStatus && isFakeProvider(aiStatus) ? "fake" : ""
            }`}
          >
            {aiStatus ? `${aiStatus.provider} - ${aiStatus.model} - ${getAvailabilityLabel(aiStatus)}` : "AI status loading"}
          </span>
        </header>

        {error && <ErrorMessage error={error} />}
        {notice && (
          <div className="message success" role="status">
            <strong>Success</strong>
            <p>{notice}</p>
          </div>
        )}

        {view === "home" && (
          <div className="panel-grid">
            <article className="panel">
              <h3>Profile readiness</h3>
              <p>{profileReadiness.contactMessage}</p>
              <p>{profileReadiness.evidenceMessage}</p>
              {profileReadiness.warnings.length > 0 && (
                <button type="button" onClick={() => setView("profile")}>
                  Review profile setup
                </button>
              )}
            </article>
            <article className="panel">
              <h3>New application</h3>
              <p>Create a saved application workspace for the posting, language, and manual workflow state.</p>
              <button className="primary-action" type="button" onClick={startNewApplication}>
                New application
              </button>
            </article>
            <article className="panel">
              <h3>Recent applications</h3>
              <p>{applications.length} application session{applications.length === 1 ? "" : "s"} saved.</p>
              {applications.length > 0 && (
                <div className="recent-next-actions">
                  {applications.slice(0, 3).map((application) => (
                    <button key={application.id} type="button" onClick={() => void openApplication(application)}>
                      <strong>{application.companyName}</strong>
                      <span>Next: {applicationNextActionLabel(application, approvedProfileFacts.length)}</span>
                    </button>
                  ))}
                </div>
              )}
            </article>
            <article className="panel">
              <h3>AI status</h3>
              {aiStatus ? (
                <ProviderReadinessSummary
                  status={aiStatus}
                  diagnostics={aiDiagnostics}
                  diagnosticsLastRanAt={aiDiagnosticsLastRanAt}
                  compact
                />
              ) : (
                <p>Loading AI provider status.</p>
              )}
            </article>
          </div>
        )}

        {view === "profile" && (
          <div className="profile-layout">
            <form className="form-layout panel-form" onSubmit={saveProfile}>
              <div className="section-heading">
                <h3>Contact and tone</h3>
                <p>{profileReadiness.contactMessage}</p>
              </div>
              {!profileReadiness.hasContactDetails && (
                <p className="workflow-note warning">Contact setup is incomplete. You can keep editing applications, but later drafts and exports may miss useful applicant context.</p>
              )}
              <Field label="Full name" required value={profile.fullName} onChange={(fullName) => setProfile({ ...profile, fullName })} />
              <Field label="Email" required type="email" value={profile.email} onChange={(email) => setProfile({ ...profile, email })} />
              <Field label="Phone" value={profile.phone} onChange={(phone) => setProfile({ ...profile, phone })} />
              <Field label="Location" value={profile.location} onChange={(location) => setProfile({ ...profile, location })} />
              <Field label="LinkedIn URL" type="url" value={profile.linkedInUrl} onChange={(linkedInUrl) => setProfile({ ...profile, linkedInUrl })} />
              <Field label="GitHub URL" type="url" value={profile.gitHubUrl} onChange={(gitHubUrl) => setProfile({ ...profile, gitHubUrl })} />
              <Field label="Portfolio URL" type="url" value={profile.portfolioUrl} onChange={(portfolioUrl) => setProfile({ ...profile, portfolioUrl })} />
              <Field label="Default language" required value={profile.defaultLanguage} onChange={(defaultLanguage) => setProfile({ ...profile, defaultLanguage })} />
              <Textarea label="Danish tone" value={profile.danishTone} onChange={(danishTone) => setProfile({ ...profile, danishTone })} />
              <Textarea label="English tone" value={profile.englishTone} onChange={(englishTone) => setProfile({ ...profile, englishTone })} />
              <div className="form-actions">
                <button className="primary-action" type="submit">Save profile</button>
              </div>
            </form>

            <section className="profile-facts-panel">
              <div className="section-heading">
                <h3>Profile facts</h3>
                <p>{profileReadiness.evidenceMessage}</p>
              </div>
              <form className="profile-import-panel" onSubmit={importProfilePdfCv}>
                <div className="section-heading">
                  <h4>Assisted CV import</h4>
                  <p>Upload a PDF CV to draft profile facts, review the grouped queue, and approve evidence for saved applications.</p>
                </div>
                {aiStatus && isFakeProvider(aiStatus) && (
                  <p className="workflow-note warning">Fake AI mode is active. Import will use deterministic demo/test extraction behavior.</p>
                )}
                <label className="file-field">
                  <span>PDF CV</span>
                  <input
                    accept="application/pdf,.pdf"
                    ref={profileImportFileRef}
                    type="file"
                    onChange={(event) => setProfileImportFile(event.target.files?.[0] ?? null)}
                  />
                </label>
                <div className="form-actions">
                  <button className="primary-action" type="submit" disabled={profileImportBusy}>
                    {profileImportBusy ? "Importing..." : "Import PDF CV"}
                  </button>
                </div>
                {profileImportFeedback && (
                  <div className={`message compact ${profileImportFeedback.tone}`} role="status">
                    <strong>{profileImportFeedback.title}</strong>
                    <p>{profileImportFeedback.message}</p>
                  </div>
                )}
              </form>
              {!profileReadiness.hasApprovedEvidence && (
                <p className="workflow-note warning">Approved evidence is required before evidence matching and draft generation. Draft or archived facts will not be used as proof.</p>
              )}
              {importReviewQueues.length > 0 && (
                <div className="import-review-queues" ref={importReviewRef}>
                  {importReviewQueues.map((queue) => (
                    <section className="import-review-queue" key={queue.importSessionId}>
                      <div className="section-heading">
                        <h4>{queue.fileName}</h4>
                        <p>{queue.draftFactCount} imported draft facts grouped for review.</p>
                      </div>
                      <div className="import-review-actions">
                        <button
                          type="button"
                          disabled={selectedIdsForQueue(queue).length < 2}
                          onClick={() => void mergeImportedDraftFacts(queue)}
                        >
                          Merge selected
                        </button>
                        <button
                          type="button"
                          disabled={selectedIdsForQueue(queue).length === 0}
                          onClick={() => void bulkReviewImportedDraftFacts(queue, "approve")}
                        >
                          Approve selected
                        </button>
                        <button
                          type="button"
                          disabled={selectedIdsForQueue(queue).length === 0}
                          onClick={() => void bulkReviewImportedDraftFacts(queue, "archive")}
                        >
                          Archive selected
                        </button>
                      </div>
                      {queue.groups.map((group) => (
                        <div className="import-review-group" key={group.key}>
                          <div className="import-review-group-header">
                            <strong>{group.label}</strong>
                            <span>{group.draftFactCount}</span>
                          </div>
                          <div className="import-review-items">
                            {group.facts.map((item) => (
                              <article
                                className={`import-review-item${item.hasDuplicateIndicators ? " duplicate" : ""}`}
                                key={item.profileFact.id}
                              >
                                <label className="import-review-select">
                                  <input
                                    type="checkbox"
                                    checked={selectedImportedDraftFactIds.includes(item.profileFact.id)}
                                    onChange={() => toggleImportedDraftFactSelection(item.profileFact.id)}
                                  />
                                  <span>Select</span>
                                </label>
                                <button type="button" onClick={() => openProfileFact(item.profileFact)}>
                                  <span className="import-review-title">{item.profileFact.title}</span>
                                  <span className="import-review-context">{item.sourceContext}</span>
                                  {item.hasDuplicateIndicators && (
                                    <span className="duplicate-indicators">
                                      {item.duplicateIndicators.map((indicator) => (
                                        <small key={`${indicator.scope}-${indicator.profileFactId}`}>
                                          {duplicateScopeLabel(indicator.scope)}: {indicator.profileFactTitle} - {indicator.reason}
                                        </small>
                                      ))}
                                    </span>
                                  )}
                                </button>
                              </article>
                            ))}
                          </div>
                        </div>
                      ))}
                    </section>
                  ))}
                </div>
              )}
              {profileFacts.length === 0 && <p className="empty-state">No profile facts yet.</p>}
              <div className="fact-list">
                {profileFacts.map((fact) => (
                  <button
                    className={`fact-card ${fact.status.toLowerCase()}${fact.id === selectedProfileFactId ? " active" : ""}`}
                    key={fact.id}
                    type="button"
                    onClick={() => openProfileFact(fact)}
                  >
                    <strong>{fact.title}</strong>
                    <span>{fact.type}</span>
                    <StatusBadge tone={statusTone(fact.status)}>{profileFactStatusLabel(fact.status)}</StatusBadge>
                  </button>
                ))}
              </div>

              <form className="form-layout fact-editor" onSubmit={saveProfileFact}>
                <Field label="Type" required value={profileFactForm.type} onChange={(type) => setProfileFactForm({ ...profileFactForm, type })} />
                <Select label="Status" required value={profileFactForm.status} options={profileFactStatuses} onChange={(status) => setProfileFactForm({ ...profileFactForm, status })} />
                <Field label="Title" required value={profileFactForm.title} onChange={(title) => setProfileFactForm({ ...profileFactForm, title })} />
                <Textarea label="Summary" required value={profileFactForm.summary} onChange={(summary) => setProfileFactForm({ ...profileFactForm, summary })} />
                <Textarea label="Fact items JSON" value={profileFactForm.factItems} onChange={(factItems) => setProfileFactForm({ ...profileFactForm, factItems })} />
                <Textarea label="Technologies JSON" value={profileFactForm.technologies} onChange={(technologies) => setProfileFactForm({ ...profileFactForm, technologies })} />
                <Textarea label="Allowed claims JSON" value={profileFactForm.allowedClaims} onChange={(allowedClaims) => setProfileFactForm({ ...profileFactForm, allowedClaims })} />
                <Textarea label="Forbidden claims JSON" value={profileFactForm.forbiddenClaims} onChange={(forbiddenClaims) => setProfileFactForm({ ...profileFactForm, forbiddenClaims })} />
                {selectedImportedDraftFact && selectedImportReviewQueue && (
                  <Textarea
                    label="Split into imported draft facts JSON"
                    value={splitImportedDraftFacts}
                    onChange={setSplitImportedDraftFacts}
                  />
                )}
                <div className="form-actions">
                  {selectedImportedDraftFact && (
                    <>
                      <button
                        className="primary-action"
                        type="button"
                        onClick={() =>
                          void reviewImportedDraftFact(
                            selectedImportedDraftFact,
                            "approve",
                            undefined,
                            profileFactFormChanged(selectedImportedDraftFact, profileFactForm) ? profileFactForm : undefined
                          )
                        }
                      >
                        Approve import
                      </button>
                      <button type="button" onClick={() => void reviewImportedDraftFact(selectedImportedDraftFact, "archive")}>
                        Archive import
                      </button>
                      <button type="button" disabled={!splitImportedDraftFacts.trim()} onClick={() => void splitImportedDraftFact()}>
                        Split import
                      </button>
                      <button className="danger-action" type="button" onClick={() => void reviewImportedDraftFact(selectedImportedDraftFact, "reject")}>
                        Reject import
                      </button>
                    </>
                  )}
                  <button className="primary-action" type="submit">{selectedProfileFactId ? "Save fact" : "Create fact"}</button>
                  <button type="button" onClick={startNewProfileFact}>Clear</button>
                  {selectedProfileFactId && (
                    <button className="danger-action" type="button" onClick={deleteProfileFact}>Delete</button>
                  )}
                </div>
              </form>
            </section>
          </div>
        )}

        {view === "applications" && (
          <div className="applications-layout">
            <section className="list-panel" aria-label="Application sessions">
              <div className="list-header">
                <h3>Applications</h3>
                <button type="button" onClick={startNewApplication}>New</button>
              </div>
              <div className="filters">
                <Field label="Search" value={applicationSearch} onChange={setApplicationSearch} />
                <Select label="Status" value={applicationStatusFilter} options={["All", ...applicationStatuses]} onChange={setApplicationStatusFilter} />
                <Select label="Draft/audit" value={applicationReadinessFilter} options={auditReadinessOptions} onChange={setApplicationReadinessFilter} />
                <label className="checkbox-field">
                  <input
                    type="checkbox"
                    checked={includeArchivedApplications}
                    onChange={(event) => setIncludeArchivedApplications(event.target.checked)}
                  />
                  <span>Include archived</span>
                </label>
              </div>
              <div className="session-list">
                {filteredApplications.map((application) => (
                  <button
                    className={`session ${applicationStatusClass(application.status)}${application.id === selectedApplicationId ? " active" : ""}`}
                    key={application.id}
                    type="button"
                    onClick={() => void openApplication(application)}
                  >
                    <StatusBadge tone={applicationHistoryTone(application.status)}>{applicationHistoryLabel(application.status)}</StatusBadge>
                    <strong>{application.companyName}</strong>
                    <span>{application.roleTitle}</span>
                    <small>
                      {applicationStatusLabel(application.status)} - {application.selectedLanguage || application.detectedLanguage || "Language unset"} - Updated {formatDate(application.updatedAt)}
                    </small>
                    <div className="session-badges">
                      {application.deadline && <span>Deadline {formatDate(application.deadline)}</span>}
                      <span className="next-action">Next: {applicationNextActionLabel(application, approvedProfileFacts.length)}</span>
                      <span className={application.hasGeneratedDraft ? "ready" : "muted"}>{application.hasGeneratedDraft ? "Draft ready" : "No draft"}</span>
                      <span className={`audit-${application.auditReadiness.toLowerCase()}`}>Audit {readinessLabel(application.auditReadiness)}</span>
                    </div>
                  </button>
                ))}
                {applications.length === 0 && <p className="empty-state">{applicationHistoryEmptyMessage(includeArchivedApplications)}</p>}
                {applications.length > 0 && filteredApplications.length === 0 && <p className="empty-state">No applications match the current search, status, and draft/audit filters.</p>}
              </div>
            </section>

            <form className="form-layout editor-panel" onSubmit={saveApplication}>
              <div className="section-heading">
                <h3>{selectedApplication ? "Application detail" : "New application"}</h3>
                <p>Capture the posting, language, and manual workflow state for this session.</p>
              </div>
              <Field label="Company name" required value={applicationForm.companyName} onChange={(companyName) => setApplicationForm({ ...applicationForm, companyName })} />
              <Field label="Role title" required value={applicationForm.roleTitle} onChange={(roleTitle) => setApplicationForm({ ...applicationForm, roleTitle })} />
              <Field label="Application URL" type="url" value={applicationForm.applicationUrl} onChange={(applicationUrl) => setApplicationForm({ ...applicationForm, applicationUrl })} />
              <Field label="Deadline" type="date" value={applicationForm.deadline} onChange={(deadline) => setApplicationForm({ ...applicationForm, deadline })} />
              <Select label="Status" required value={applicationForm.status} options={applicationStatuses} onChange={(status) => setApplicationForm({ ...applicationForm, status })} />
              <Field label="Detected language" value={applicationForm.detectedLanguage} onChange={(detectedLanguage) => setApplicationForm({ ...applicationForm, detectedLanguage })} />
              <Field label="Selected language" value={applicationForm.selectedLanguage} onChange={(selectedLanguage) => setApplicationForm({ ...applicationForm, selectedLanguage })} />
              <Textarea label="Job posting text" value={applicationForm.jobPostingText} onChange={(jobPostingText) => setApplicationForm({ ...applicationForm, jobPostingText })} />
              <div className="form-actions">
                <button className="primary-action" type="submit">{selectedApplication ? "Save application" : "Create application"}</button>
                <button type="button" onClick={startNewApplication}>Clear</button>
                {selectedApplicationId && (
                  <button className="danger-action" type="button" onClick={deleteApplication}>Delete</button>
                )}
              </div>
              {selectedApplicationId && (
                <div className="final-status-actions" aria-label="Final application status actions">
                  <button
                    type="button"
                    onClick={() => void markApplicationStatus("Applied")}
                    disabled={selectedApplication?.status === "Applied" || workflowBusy !== null}
                    title={disabledTitle(
                      selectedApplication?.status === "Applied" || workflowBusy !== null,
                      selectedApplication?.status === "Applied" ? "This application is already marked applied." : workflowBusyReason
                    )}
                  >
                    Mark applied
                  </button>
                  <button
                    className="danger-action"
                    type="button"
                    onClick={() => void markApplicationStatus("Archived")}
                    disabled={selectedApplication?.status === "Archived" || workflowBusy !== null}
                    title={disabledTitle(
                      selectedApplication?.status === "Archived" || workflowBusy !== null,
                      selectedApplication?.status === "Archived" ? "This application is already archived." : workflowBusyReason
                    )}
                  >
                    Archive
                  </button>
                </div>
              )}

              <section className="workflow-panel">
                <div className="section-heading">
                  <h3>Application text workflow</h3>
                  <p>Move from job analysis to approved evidence, generated text, and claim audit before final use.</p>
                </div>
                {aiStatus && (
                  <ProviderReadinessSummary
                    status={aiStatus}
                    diagnostics={aiDiagnostics}
                    diagnosticsLastRanAt={aiDiagnosticsLastRanAt}
                  />
                )}
                <section className={`guided-action ${guidedNextAction.tone}`} aria-label="Guided next action">
                  <div>
                    <span>Next action</span>
                    <h4>{guidedNextAction.title}</h4>
                    <p>{guidedNextAction.message}</p>
                  </div>
                  <button
                    className="primary-action"
                    type={guidedNextAction.kind === "save-posting" ? "submit" : "button"}
                    onClick={guidedNextAction.kind === "save-posting" ? undefined : runGuidedNextAction}
                    disabled={!guidedNextAction.canRun || workflowBusy !== null}
                    title={disabledTitle(!guidedNextAction.canRun || workflowBusy !== null, workflowBusyReason ?? guidedNextAction.message)}
                  >
                    {guidedActionButtonLabel(guidedNextAction.buttonLabel, guidedNextAction.kind, workflowBusy)}
                  </button>
                </section>
                {selectedApplication && (
                  <div className="trust-chain" aria-label="Draft trust chain">
                    <StatusBadge tone={preparationStatusTone(selectedApplication.preparationStatus)}>
                      {preparationStatusLabel(selectedApplication.preparationStatus)}
                    </StatusBadge>
                    <StatusBadge tone={hasSavedApprovedEvidence ? "approved" : "pending"}>
                      {approvedEvidenceCountLabel(savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount)}
                    </StatusBadge>
                    <StatusBadge tone={hasGeneratedDraft ? "approved" : "draft"}>
                      {hasGeneratedDraft ? "Draft saved" : "No draft"}
                    </StatusBadge>
                    <StatusBadge tone={auditReadinessTone(effectiveAuditReadiness)}>
                      {auditReadinessLabel(effectiveAuditReadiness)}
                    </StatusBadge>
                    <StatusBadge tone={coverLetterExportState.canExport ? "approved" : "pending"}>
                      {coverLetterExportState.canExport ? "Export ready" : "Export blocked"}
                    </StatusBadge>
                  </div>
                )}

                <div className="workflow-step">
                  <div>
                    <h4>1. Job analysis</h4>
                    <p>{jobAnalysisState.message}</p>
                  </div>
                  <button
                    className="secondary-workflow-action"
                    type="button"
                    onClick={analyzeJob}
                    disabled={!jobAnalysisState.canRun || workflowBusy !== null}
                    title={disabledTitle(!jobAnalysisState.canRun || workflowBusy !== null, workflowBusyReason ?? jobAnalysisState.message)}
                  >
                    {workflowBusy === "analysis" ? "Analyzing..." : "Analyze job"}
                  </button>
                </div>

                {jobSignals.signals.length > 0 ? (
                  <div className="signal-grid">
                    <SignalColumn title="Required skills" values={jobSignals.requiredSkills} />
                    <SignalColumn title="Preferred skills" values={jobSignals.preferredSkills} />
                    <SignalColumn title="Responsibilities" values={jobSignals.responsibilities} />
                  </div>
                ) : (
                  <p className="empty-state compact">No analysis results yet.</p>
                )}

                <div className="workflow-step">
                  <div>
                    <h4>2. Evidence matching</h4>
                    <p>{evidenceMatchingState.message}</p>
                  </div>
                  <button
                    className="secondary-workflow-action"
                    type="button"
                    onClick={matchEvidence}
                    disabled={!evidenceMatchingState.canRun || workflowBusy !== null}
                    title={disabledTitle(!evidenceMatchingState.canRun || workflowBusy !== null, workflowBusyReason ?? evidenceMatchingState.message)}
                  >
                    {workflowBusy === "matching" ? "Matching..." : "Match evidence"}
                  </button>
                </div>

                <div className="review-grid" ref={evidenceReviewRef}>
                  <section className="review-column">
                    <h4>Matched evidence</h4>
                    {evidenceMatches.length === 0 && <p className="empty-state compact">No matches yet.</p>}
                    {evidenceMatches.map((match) => (
                      <article className="evidence-card" key={match.id}>
                        <strong>{match.signal}</strong>
                        <span>{match.profileFactTitle}</span>
                        <p>{match.summary}</p>
                        <small>Matched: {match.matchedTerms.join(", ")}</small>
                        <button type="button" onClick={() => approveMatch(match)}>Approve</button>
                      </article>
                    ))}
                  </section>

                  <section className="review-column">
                    <h4>Unmatched requirements</h4>
                    {unmatchedRequirements.length === 0 && <p className="empty-state compact">No unmatched requirements recorded.</p>}
                    {unmatchedRequirements.map((requirement) => {
                      const gapDecision = gapDecisionForRequirement(gapDecisionsDraft, requirement.id);
                      const requirementCustomFacts = customFactsForRequirement(customFacts, requirement.id);
                      const approvedCustomFacts = requirementCustomFacts.filter((fact) => fact.status === "Approved");
                      const customFactDraft = customFactDrafts[requirement.id] ?? emptyCustomFactDraft();

                      return (
                        <article className="evidence-card muted" key={requirement.id}>
                          <div className="gap-card-heading">
                            <strong>{requirement.requirement}</strong>
                            {gapDecision ? (
                              <StatusBadge tone={gapDecision.decision === "Ignore" ? "neutral" : "pending"}>
                                {gapDecisionLabel(gapDecision.decision)}
                              </StatusBadge>
                            ) : (
                              <StatusBadge tone="pending">Needs decision</StatusBadge>
                            )}
                          </div>
                          <span>{requirement.category}</span>
                          <p>{requirement.recommendation}</p>
                          <div className="gap-decision-actions" role="group" aria-label={`Gap decision for ${requirement.requirement}`}>
                            <button
                              type="button"
                              className={gapDecision?.decision === "Ignore" ? "selected" : ""}
                              onClick={() => decideGap(requirement.id, "Ignore")}
                            >
                              Ignore
                            </button>
                            <button
                              type="button"
                              className={gapDecision?.decision === "MentionAsLearningInterest" ? "selected" : ""}
                              onClick={() => decideGap(requirement.id, "MentionAsLearningInterest")}
                            >
                              Mention as learning interest
                            </button>
                            {approvedCustomFacts.map((fact) => (
                              <button
                                type="button"
                                className={gapDecision?.decision === "CoveredByCustomFact" && gapDecision.customFactId === fact.id ? "selected" : ""}
                                key={fact.id}
                                onClick={() => decideGap(requirement.id, "CoveredByCustomFact", fact.id)}
                              >
                                Cover with {fact.title}
                              </button>
                            ))}
                          </div>

                          <div className="custom-fact-editor">
                            <Field
                              label="Custom fact title"
                              value={customFactDraft.title}
                              onChange={(title) => updateCustomFactDraft(requirement.id, { title })}
                            />
                            <Textarea
                              label="Custom fact summary"
                              value={customFactDraft.summary}
                              onChange={(summary) => updateCustomFactDraft(requirement.id, { summary })}
                            />
                            <Textarea
                              label="Technologies"
                              value={customFactDraft.technologies}
                              onChange={(technologies) => updateCustomFactDraft(requirement.id, { technologies })}
                            />
                            <Textarea
                              label="Allowed claims"
                              value={customFactDraft.allowedClaims}
                              onChange={(allowedClaims) => updateCustomFactDraft(requirement.id, { allowedClaims })}
                            />
                            <button
                              type="button"
                              onClick={() => createCustomFact(requirement.id)}
                              disabled={workflowBusy !== null}
                              title={disabledTitle(workflowBusy !== null, workflowBusyReason)}
                            >
                              {workflowBusy === `custom-fact-${requirement.id}` ? "Adding..." : "Add job-local fact"}
                            </button>
                          </div>

                          {requirementCustomFacts.length > 0 && (
                            <div className="custom-fact-list compact">
                              {requirementCustomFacts.map((fact) => (
                                <article className={`custom-fact ${customFactStatusClass(fact.status)}`} key={fact.id}>
                                  <StatusBadge tone={customFactStatusTone(fact.status)}>{customFactStatusLabel(fact.status)}</StatusBadge>
                                  <strong>{fact.title}</strong>
                                  <p>{fact.summary}</p>
                                  {fact.technologies && fact.technologies.length > 0 && <small>{fact.technologies.join(", ")}</small>}
                                  {fact.status === "PendingConfirmation" && (
                                    <div className="custom-fact-actions">
                                      <button
                                        type="button"
                                        onClick={() => updateCustomFactStatus(fact.id, "Approved")}
                                        disabled={workflowBusy !== null}
                                        title={disabledTitle(workflowBusy !== null, workflowBusyReason)}
                                      >
                                        Approve
                                      </button>
                                      <button
                                        type="button"
                                        onClick={() => updateCustomFactStatus(fact.id, "Rejected")}
                                        disabled={workflowBusy !== null}
                                        title={disabledTitle(workflowBusy !== null, workflowBusyReason)}
                                      >
                                        Reject
                                      </button>
                                    </div>
                                  )}
                                </article>
                              ))}
                            </div>
                          )}
                        </article>
                      );
                    })}
                  </section>
                </div>

                <div className="workflow-step">
                  <div>
                    <h4>3. Evidence review decisions</h4>
                    <p>Save approved evidence as support for generated claims. Gap decisions are review guidance, not approved evidence.</p>
                  </div>
                  <button
                    className="secondary-workflow-action"
                    type="button"
                    onClick={saveApprovedEvidence}
                    disabled={!selectedApplicationId || workflowBusy !== null}
                    title={disabledTitle(
                      !selectedApplicationId || workflowBusy !== null,
                      workflowBusyReason ?? "Save the application before reviewing evidence."
                    )}
                  >
                    {workflowBusy === "review" ? "Saving..." : "Save evidence review"}
                  </button>
                </div>

                <div className="approved-list">
                  {approvedEvidenceDraft.length === 0 && savedApprovedCustomFactEvidence.length === 0 && (
                    <p className="empty-state compact">No approved evidence selected.</p>
                  )}
                  {approvedEvidenceDraft.map((match) => (
                    <article className="approved-item" key={match.id}>
                      <div>
                        <StatusBadge tone="approved">Approved profile evidence</StatusBadge>
                        <strong>{match.signal}</strong>
                        <span>{match.profileFactTitle}</span>
                      </div>
                      <button type="button" onClick={() => removeApprovedEvidence(match.id)}>Remove</button>
                    </article>
                  ))}
                  {savedApprovedCustomFactEvidence.map((item) => (
                    <article className="approved-item custom-proof" key={item.fact.id}>
                      <div>
                        <StatusBadge tone="approved">Approved job-local fact</StatusBadge>
                        <strong>{item.requirement.requirement}</strong>
                        <span>{item.fact.title}</span>
                      </div>
                    </article>
                  ))}
                </div>

                <div className="gap-decision-list">
                  {gapDecisionsDraft.length === 0 && <p className="empty-state compact">No gap decisions selected.</p>}
                  {gapDecisionsDraft.map((decision) => {
                    const requirement = unmatchedRequirements.find((item) => item.id === decision.unmatchedRequirementId);

                    return (
                      <article className={`gap-decision-item ${gapDecisionClass(decision)}`} key={decision.unmatchedRequirementId}>
                        <div>
                          <StatusBadge tone={gapDecisionTone(decision)}>
                            {gapDecisionLabel(decision.decision)}
                          </StatusBadge>
                          <strong>{requirement?.requirement ?? decision.unmatchedRequirementId}</strong>
                          <span>{gapDecisionSummary(decision, customFacts)}</span>
                        </div>
                      </article>
                    );
                  })}
                </div>

                <section className="custom-facts-panel">
                  <div className="section-heading">
                    <h4>Job-local custom facts</h4>
                    <p>Only approved job-local facts should support generated claims.</p>
                  </div>
                  {customFacts.length === 0 ? (
                    <p className="empty-state compact">No job-local custom facts recorded.</p>
                  ) : (
                    <div className="custom-fact-list">
                      {customFacts.map((fact) => (
                        <article className={`custom-fact ${customFactStatusClass(fact.status)}`} key={fact.id}>
                          <StatusBadge tone={customFactStatusTone(fact.status)}>{customFactStatusLabel(fact.status)}</StatusBadge>
                          <strong>{fact.title}</strong>
                          <p>{fact.summary}</p>
                          {fact.technologies && fact.technologies.length > 0 && <small>{fact.technologies.join(", ")}</small>}
                        </article>
                      ))}
                    </div>
                  )}
                </section>

                <div className="workflow-step">
                  <div>
                    <h4>4. Generated draft</h4>
                    <p>{draftGenerationState.message}</p>
                    {aiStatus && (
                      <span className={`inline-readiness ${aiStatus.isAvailable ? "available" : "unavailable"} ${isFakeProvider(aiStatus) ? "fake" : ""}`}>
                        {getDraftReadinessLabel(aiStatus)}
                      </span>
                    )}
                  </div>
                  <button
                    className="secondary-workflow-action"
                    type="button"
                    onClick={generateDraft}
                    disabled={!draftGenerationState.canRun || workflowBusy !== null}
                    title={disabledTitle(!draftGenerationState.canRun || workflowBusy !== null, workflowBusyReason ?? draftGenerationState.message)}
                  >
                    {workflowBusy === "draft" ? "Generating..." : "Generate and audit draft"}
                  </button>
                </div>

                {selectedApplication?.generatedDraft ? (
                  <section className="draft-editor" ref={draftReviewRef}>
                    <div className="section-heading">
                      <h4>Current draft</h4>
                      <p>
                        Generated {formatDate(selectedApplication.generatedDraft.generatedAt)}
                        {selectedApplication.generatedDraft.lastEditedAt ? ` - Edited ${formatDate(selectedApplication.generatedDraft.lastEditedAt)}` : ""}
                        {effectiveAuditReadiness === "Stale" ? " - Audit stale" : ""}
                      </p>
                    </div>
                    {aiStatus && isFakeProvider(aiStatus) && (
                      <p className="workflow-note warning">Fake AI mode: this draft uses deterministic demo/test output.</p>
                    )}
                    {effectiveAuditExportNotice && <p className={`workflow-note ${effectiveAuditExportNotice.tone}`}>{effectiveAuditExportNotice.message}</p>}
                    <Textarea
                      label="Cover letter"
                      value={generatedDraftForm.coverLetterText}
                      onChange={(coverLetterText) => {
                        setGeneratedDraftForm({ ...generatedDraftForm, coverLetterText });
                        setExportFeedback(null);
                      }}
                    />
                    <Textarea
                      label="Short motivation"
                      value={generatedDraftForm.shortMotivationText}
                      onChange={(shortMotivationText) => {
                        setGeneratedDraftForm({ ...generatedDraftForm, shortMotivationText });
                        setExportFeedback(null);
                      }}
                    />
                    <div className="form-actions">
                      <button
                        className="secondary-workflow-action"
                        type="button"
                        onClick={saveGeneratedDraft}
                        disabled={!hasUnsavedDraftEdits || workflowBusy !== null}
                        title={disabledTitle(
                          !hasUnsavedDraftEdits || workflowBusy !== null,
                          workflowBusyReason ?? "Edit the draft before saving changes."
                        )}
                      >
                        {workflowBusy === "draft-edit" ? "Saving..." : "Save draft edits"}
                      </button>
                      <button
                        type="button"
                        onClick={() => void refreshClaimAudit()}
                        disabled={!canRefreshClaimAudit || workflowBusy !== null}
                        title={disabledTitle(
                          !canRefreshClaimAudit || workflowBusy !== null,
                          workflowBusyReason ??
                            (isRealProviderUnavailable
                              ? "Open AI settings before refreshing claim audit."
                              : "Claim audit is current.")
                        )}
                      >
                        {workflowBusy === "audit" ? "Auditing..." : "Refresh claim audit"}
                      </button>
                    </div>
                    <section className="export-panel" ref={exportPanelRef}>
                      <div className="section-heading">
                        <h4>Export cover letter</h4>
                        <p>{coverLetterExportState.reason ?? "Copy or download the current saved cover letter exactly as edited."}</p>
                      </div>
                      <p className="workflow-note info">Copy uses the visible edited text. TXT and DOCX downloads use the current saved draft edits and never regenerate or re-run claim audit.</p>
                      {effectiveAuditExportNotice && <p className={`workflow-note ${effectiveAuditExportNotice.tone}`}>{effectiveAuditExportNotice.message}</p>}
                      {!coverLetterExportState.canCopy && coverLetterExportState.canExport && (
                        <p className="workflow-note neutral">Clipboard copy is not available in this browser. TXT and DOCX export are still available.</p>
                      )}
                      {exportFeedback && <InlineFeedbackMessage feedback={exportFeedback} />}
                      <div className="form-actions">
                        <button
                          type="button"
                          onClick={() => void copyCoverLetter()}
                          disabled={!coverLetterExportState.canCopy || exportBusy !== null}
                          title={disabledTitle(
                            !coverLetterExportState.canCopy || exportBusy !== null,
                            exportBusyReason ?? coverLetterExportState.reason ?? "Clipboard copy is not available in this browser."
                          )}
                        >
                          Copy
                        </button>
                        <button
                          type="button"
                          onClick={() => void downloadCoverLetter("txt")}
                          disabled={!coverLetterExportState.canExport || exportBusy !== null}
                          title={disabledTitle(!coverLetterExportState.canExport || exportBusy !== null, exportBusyReason ?? coverLetterExportState.reason)}
                        >
                          {exportBusy === "txt" ? "Downloading..." : "Download TXT"}
                        </button>
                        <button
                          type="button"
                          onClick={() => void downloadCoverLetter("docx")}
                          disabled={!coverLetterExportState.canExport || exportBusy !== null}
                          title={disabledTitle(!coverLetterExportState.canExport || exportBusy !== null, exportBusyReason ?? coverLetterExportState.reason)}
                        >
                          {exportBusy === "docx" ? "Downloading..." : "Download DOCX"}
                        </button>
                      </div>
                    </section>
                    <section className="claim-audit">
                      <div className="section-heading">
                        <h4>Claim audit</h4>
                        <p>
                          {selectedApplication.generatedDraft.auditUpdatedAt
                            ? `Updated ${formatDate(selectedApplication.generatedDraft.auditUpdatedAt)} - ${auditSummary.supported} supported, ${auditSummary.unsupported} unsupported, ${auditSummary.needsReview} needs review`
                            : "Run claim audit after the generated text is ready."}
                        </p>
                      </div>
                      {effectiveAuditExportNotice && <p className={`workflow-note ${effectiveAuditExportNotice.tone}`}>{effectiveAuditExportNotice.message}</p>}
                      {claimAudit.claims.length === 0 ? (
                        <p className="empty-state compact">No claim audit results yet.</p>
                      ) : (
                        <div className="audit-list">
                          {claimAudit.claims.map((claim) => (
                            <article className={`audit-item ${claim.status.toLowerCase()}`} key={claim.id}>
                              <span>{claim.status}</span>
                              <p>{claim.text}</p>
                              {claim.evidenceIds.length > 0 && <small>Evidence: {claim.evidenceIds.join(", ")}</small>}
                            </article>
                          ))}
                        </div>
                      )}
                    </section>
                  </section>
                ) : (
                  <section className="export-panel disabled">
                    <div className="section-heading">
                      <h4>Export cover letter</h4>
                      <p>{coverLetterExportState.reason ?? claimAuditMessage(hasGeneratedDraft)}</p>
                    </div>
                    <p className="workflow-note neutral">TXT and DOCX downloads become available after a non-empty generated draft is saved.</p>
                    <div className="form-actions">
                      <button type="button" disabled title={coverLetterExportState.reason ?? "Generate a draft before copying."}>Copy</button>
                      <button type="button" disabled title={coverLetterExportState.reason ?? "Generate a draft before downloading TXT."}>Download TXT</button>
                      <button type="button" disabled title={coverLetterExportState.reason ?? "Generate a draft before downloading DOCX."}>Download DOCX</button>
                    </div>
                  </section>
                )}
              </section>
            </form>
          </div>
        )}

        {view === "settings" && (
          <article className="panel settings-panel">
            <h3>AI settings</h3>
            {aiStatus ? (
              <>
                <p>{getProviderSummary(aiStatus)}</p>
                <dl className="status-details">
                  <div>
                    <dt>Provider</dt>
                    <dd>{aiStatus.provider}</dd>
                  </div>
                  <div>
                    <dt>Model</dt>
                    <dd>{aiStatus.model}</dd>
                  </div>
                  <div>
                    <dt>Endpoint</dt>
                    <dd>{aiStatus.endpoint ?? "Not applicable"}</dd>
                  </div>
                  <div>
                    <dt>Availability</dt>
                    <dd>{aiStatus.isAvailable ? "Available" : "Unavailable"}</dd>
                  </div>
                  <div>
                    <dt>Diagnostics</dt>
                    <dd>{aiDiagnosticsLastRanAt ? `Last ran ${formatDateTime(aiDiagnosticsLastRanAt)}` : "Not run this session"}</dd>
                  </div>
                </dl>
              </>
            ) : (
              <p>Loading AI provider status.</p>
            )}
            <button className="primary-action" type="button" onClick={runAiDiagnostics} disabled={aiDiagnosticsBusy}>
              {aiDiagnosticsBusy ? "Running diagnostics..." : "Run diagnostics"}
            </button>
            {aiDiagnosticsBusy && <p className="diagnostics-state">Checking provider connectivity and model readiness.</p>}
            {!aiDiagnosticsBusy && !aiDiagnostics && !aiDiagnosticsError && (
              <p className="diagnostics-state">Diagnostics have not been run this session.</p>
            )}
            {aiDiagnosticsError && <ErrorMessage error={aiDiagnosticsError} compact />}
            {aiDiagnostics && (
              <section className="diagnostics-list">
                <h4>Diagnostics result</h4>
                <p className={aiDiagnostics.isAvailable ? "diagnostics-state success" : "diagnostics-state warning"}>
                  {aiDiagnostics.message}
                </p>
                {aiDiagnostics.checks.map((check) => (
                  <article className="diagnostics-item" key={check.name}>
                    <strong>{check.name}</strong>
                    <span>{check.status}</span>
                    <p>{check.message}</p>
                  </article>
                ))}
              </section>
            )}
          </article>
        )}
      </section>
    </main>
  );
}

function Field(props: {
  label: string;
  type?: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <input
        type={props.type ?? "text"}
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </label>
  );
}

function Select(props: {
  label: string;
  required?: boolean;
  value: string;
  options: string[];
  onChange: (value: string) => void;
}) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <select
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      >
        {props.options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </label>
  );
}

function Textarea(props: {
  label: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field wide">
      <span>{props.label}</span>
      <textarea required={props.required} value={props.value} onChange={(event) => props.onChange(event.target.value)} />
    </label>
  );
}

function ErrorMessage(props: { error: ErrorPresentation; compact?: boolean }) {
  return (
    <div className={props.compact ? "message error compact" : "message error"} role="alert">
      <strong>{props.error.title}</strong>
      <p>{props.error.message}</p>
      {props.error.details && props.error.details.length > 0 && (
        <details>
          <summary>Technical details</summary>
          <ul>
            {props.error.details.map((detail, index) => (
              <li key={`${detail}-${index}`}>{detail}</li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}

function InlineFeedbackMessage(props: { feedback: InlineFeedback }) {
  return (
    <div className={`workflow-note ${props.feedback.tone}`} role={props.feedback.tone === "error" ? "alert" : "status"}>
      <strong>{props.feedback.title}</strong>
      <p>{props.feedback.message}</p>
      {props.feedback.details && props.feedback.details.length > 0 && (
        <details>
          <summary>Technical details</summary>
          <ul>
            {props.feedback.details.map((detail, index) => (
              <li key={`${detail}-${index}`}>{detail}</li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}

function StatusBadge(props: { tone: string; children: string }) {
  return <span className={`state-badge ${props.tone}`}>{props.children}</span>;
}

function SignalColumn(props: { title: string; values: string[] }) {
  return (
    <section className="signal-column">
      <h4>{props.title}</h4>
      {props.values.length === 0 ? (
        <p>None found.</p>
      ) : (
        <ul>
          {props.values.map((value) => (
            <li key={value}>{value}</li>
          ))}
        </ul>
      )}
    </section>
  );
}

async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`);
  return readResponse<T>(response);
}

async function apiSend<T>(path: string, method: "POST" | "PUT", body: unknown): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return readResponse<T>(response);
}

async function apiSendForm<T>(path: string, body: FormData): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    body
  });
  return readResponse<T>(response);
}

async function apiDelete(path: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: "DELETE" });
  if (!response.ok) {
    throw await response.json();
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  const body = await response.json();
  if (!response.ok) {
    throw body;
  }
  return body;
}

function toProfileForm(profile: ProfileForm): ProfileForm {
  return {
    ...emptyProfile,
    ...profile,
    phone: profile.phone ?? "",
    location: profile.location ?? "",
    linkedInUrl: profile.linkedInUrl ?? "",
    gitHubUrl: profile.gitHubUrl ?? "",
    portfolioUrl: profile.portfolioUrl ?? "",
    danishTone: profile.danishTone ?? "",
    englishTone: profile.englishTone ?? ""
  };
}

function toProfileFactForm(fact: ProfileFactForm): ProfileFactForm {
  return {
    ...emptyProfileFact,
    ...fact,
    factItems: fact.factItems || "[]",
    technologies: fact.technologies || "[]",
    allowedClaims: fact.allowedClaims || "[]",
    forbiddenClaims: fact.forbiddenClaims || "[]"
  };
}

function toProfileFact(fact: ProfileFact): ProfileFact {
  return {
    ...toProfileFactForm(fact),
    id: fact.id,
    createdAt: fact.createdAt,
    updatedAt: fact.updatedAt,
    sourceDocumentIds: fact.sourceDocumentIds ?? "[]",
    originalImportedSnapshot: fact.originalImportedSnapshot ?? null,
    manuallyEdited: Boolean(fact.manuallyEdited)
  };
}

function importSessionIdFromProfileFact(fact: ProfileFact): string | null {
  if (!fact.originalImportedSnapshot || fact.status !== "Draft") {
    return null;
  }

  try {
    const snapshot = JSON.parse(fact.originalImportedSnapshot) as { importSessionId?: string };
    return snapshot.importSessionId ?? null;
  } catch {
    return null;
  }
}

function duplicateScopeLabel(scope: string): string {
  if (scope === "ImportBatch") {
    return "Same import";
  }

  if (scope === "ExistingProfileFact") {
    return "Existing fact";
  }

  return "Overlap";
}

function importDecisionPastTense(decision: "approve" | "archive" | "reject"): string {
  switch (decision) {
    case "approve":
      return "approved";
    case "archive":
      return "archived";
    case "reject":
      return "rejected";
  }
}

function profileFactFormChanged(fact: ProfileFact, form: ProfileFactForm): boolean {
  return (
    fact.type !== form.type ||
    fact.title !== form.title ||
    fact.summary !== form.summary ||
    fact.status !== form.status ||
    fact.factItems !== form.factItems ||
    fact.technologies !== form.technologies ||
    fact.allowedClaims !== form.allowedClaims ||
    fact.forbiddenClaims !== form.forbiddenClaims
  );
}

function splitDraftTemplate(fact: ProfileFact): string {
  const first = toProfileFactForm(fact);
  const second = toProfileFactForm(fact);
  first.title = `${fact.title} - part 1`;
  second.title = `${fact.title} - part 2`;
  first.summary = "";
  second.summary = "";

  return JSON.stringify([first, second], null, 2);
}

function toApplicationForm(application: ApplicationForm) {
  return {
    ...application,
    applicationUrl: application.applicationUrl ?? "",
    deadline: application.deadline ?? "",
    jobPostingText: application.jobPostingText ?? "",
    detectedLanguage: application.detectedLanguage ?? "",
    selectedLanguage: application.selectedLanguage ?? ""
  };
}

function toApplicationSession(application: ApplicationSession): ApplicationSession {
  return {
    ...toApplicationForm(application),
    id: application.id,
    jobSignals: application.jobSignals || "{}",
    evidenceMatches: application.evidenceMatches || "[]",
    unmatchedRequirements: application.unmatchedRequirements || "[]",
    approvedEvidence: application.approvedEvidence || "[]",
    gapDecisions: application.gapDecisions || "[]",
    customFacts: application.customFacts || "[]",
    lastPreparedAt: application.lastPreparedAt ?? null,
    preparationStatus: application.preparationStatus ?? "NotStarted",
    generatedDraft: application.generatedDraft ?? null,
    hasGeneratedDraft: application.hasGeneratedDraft ?? Boolean(application.generatedDraft),
    auditReadiness: application.auditReadiness ?? auditReadinessForDraft(application.generatedDraft),
    createdAt: application.createdAt,
    updatedAt: application.updatedAt
  };
}

function toApplicationPayload(application: ApplicationForm) {
  return {
    ...application,
    applicationUrl: application.applicationUrl || null,
    deadline: application.deadline || null,
    detectedLanguage: application.detectedLanguage || null,
    selectedLanguage: application.selectedLanguage || null
  };
}

function fileNameFromContentDisposition(header: string | null, fallback: string): string {
  if (!header) {
    return fallback;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const fileNameMatch = /filename="?([^";]+)"?/i.exec(header);
  return fileNameMatch?.[1] ?? fallback;
}

function ProviderReadinessSummary({
  status,
  diagnostics,
  diagnosticsLastRanAt,
  compact = false
}: {
  status: AiProviderStatus;
  diagnostics: AiDiagnostics | null;
  diagnosticsLastRanAt: string | null;
  compact?: boolean;
}) {
  const tone = getReadinessTone(status);
  const details = diagnostics?.checks ?? [];

  return (
    <section className={`provider-readiness workflow-note ${tone}`}>
      <strong>{getProviderReadinessTitle(status)}</strong>
      <p>{getProviderSummary(status)}</p>
      {!status.isAvailable && !isFakeProvider(status) && (
        <p>{getProviderRecoveryGuidance(status)}</p>
      )}
      <dl className={`status-details ${compact ? "compact" : ""}`}>
        <div>
          <dt>Provider</dt>
          <dd>{status.provider}</dd>
        </div>
        <div>
          <dt>Mode</dt>
          <dd>{isFakeProvider(status) ? "Deterministic demo/test behavior" : "Configured real provider"}</dd>
        </div>
        <div>
          <dt>Model</dt>
          <dd>{status.model}</dd>
        </div>
        <div>
          <dt>Endpoint</dt>
          <dd>{status.endpoint ?? "Not applicable"}</dd>
        </div>
        <div>
          <dt>Availability</dt>
          <dd>{status.isAvailable ? "Available" : "Unavailable"}</dd>
        </div>
        <div>
          <dt>Diagnostics</dt>
          <dd>{diagnosticsLastRanAt ? `Last ran ${formatDateTime(diagnosticsLastRanAt)}` : "Not run this session"}</dd>
        </div>
      </dl>
      {details.length > 0 && (
        <details>
          <summary>Readiness checks</summary>
          <ul>
            {details.map((check) => (
              <li key={`${check.name}-${check.status}`}>
                <strong>{check.name}</strong>: {check.status} - {check.message}
              </li>
            ))}
          </ul>
        </details>
      )}
    </section>
  );
}

function claimAuditMessage(hasGeneratedDraft: boolean): string {
  return hasGeneratedDraft
    ? "Run claim audit after reviewing the generated text."
    : "Generate a draft before running claim audit.";
}

function auditReadinessForDraft(draft: GeneratedDraft | null): string {
  if (!draft) {
    return "NotApplicable";
  }

  if (draft.isClaimAuditStale) {
    return "Stale";
  }

  if (!draft.auditUpdatedAt || draft.claimAudit === "{}") {
    return "Missing";
  }

  return "Current";
}

function readinessLabel(readiness: string): string {
  return readiness === "NotApplicable" ? "not applicable" : readiness.toLowerCase();
}

function approvedEvidenceCountLabel(count: number): string {
  return count === 1 ? "1 approved evidence item" : `${count} approved evidence items`;
}

function auditReadinessLabel(readiness: string): string {
  return `Audit ${readinessLabel(readiness)}`;
}

function auditReadinessTone(readiness: string): string {
  switch (readiness) {
    case "Current":
      return "approved";
    case "Stale":
    case "Missing":
      return "pending";
    default:
      return "neutral";
  }
}

function applicationHistoryLabel(status: string): string {
  if (status === "Applied") {
    return "Applied";
  }

  if (status === "Archived") {
    return "Archived";
  }

  return "Active";
}

function applicationHistoryTone(status: string): string {
  if (status === "Applied") {
    return "applied";
  }

  if (status === "Archived") {
    return "archived";
  }

  return "active-work";
}

function applicationStatusClass(status: string): string {
  if (status === "Applied") {
    return "applied";
  }

  if (status === "Archived") {
    return "archived";
  }

  return "active-work";
}

function applicationStatusLabel(status: string): string {
  switch (status) {
    case "PostingCaptured":
      return "Posting captured";
    case "ReadyForReview":
      return "Ready for review";
    default:
      return status;
  }
}

function applicationNextActionLabel(application: ApplicationSession, approvedProfileFactCount: number): string {
  const unmatchedRequirements = parseJsonArray<UnmatchedRequirement>(application.unmatchedRequirements);
  const gapDecisions = parseJsonArray<GapDecision>(application.gapDecisions);
  const customFacts = parseJsonArray<CustomFact>(application.customFacts);

  return getGuidedNextAction({
    selectedApplicationId: application.id,
    hasSavedJobPosting: Boolean(application.jobPostingText.trim()),
    preparationStatus: application.preparationStatus,
    approvedProfileFactCount,
    savedApprovedEvidenceCount:
      parseJsonArray<EvidenceMatch>(application.approvedEvidence).length +
      countApprovedCustomFactEvidence(gapDecisions, unmatchedRequirements, customFacts),
    unmatchedRequirementCount: unmatchedRequirements.length,
    savedGapDecisionCount: countCurrentGapDecisions(gapDecisions, unmatchedRequirements, customFacts),
    hasGeneratedDraft: application.hasGeneratedDraft,
    auditReadiness: application.auditReadiness ?? auditReadinessForDraft(application.generatedDraft),
    canCopyOrExport: Boolean(application.generatedDraft?.coverLetterText.trim())
  }).title;
}

function countCurrentGapDecisions(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[] = []
): number {
  const requirementIds = new Set(requirements.map((requirement) => requirement.id));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );
  const decidedRequirementIds = new Set(
    decisions
      .filter((decision) => {
        if (!requirementIds.has(decision.unmatchedRequirementId)) {
          return false;
        }

        if (decision.decision !== "CoveredByCustomFact") {
          return true;
        }

        const customFact = decision.customFactId ? approvedCustomFactsById.get(decision.customFactId) : null;
        return customFact?.unmatchedRequirementId === decision.unmatchedRequirementId;
      })
      .map((decision) => decision.unmatchedRequirementId)
  );

  return decidedRequirementIds.size;
}

function countApprovedCustomFactEvidence(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[]
): number {
  const requirementIds = new Set(requirements.map((requirement) => requirement.id));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );

  return new Set(
    decisions
      .filter((decision) => {
        if (decision.decision !== "CoveredByCustomFact" || !requirementIds.has(decision.unmatchedRequirementId)) {
          return false;
        }

        const customFact = decision.customFactId ? approvedCustomFactsById.get(decision.customFactId) : null;
        return customFact?.unmatchedRequirementId === decision.unmatchedRequirementId;
      })
      .map((decision) => decision.unmatchedRequirementId)
  ).size;
}

function approvedCustomFactEvidenceItems(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[]
): Array<{ requirement: UnmatchedRequirement; fact: CustomFact }> {
  const requirementsById = new Map(requirements.map((requirement) => [requirement.id, requirement]));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );

  return decisions
    .filter((decision) => decision.decision === "CoveredByCustomFact" && decision.customFactId)
    .map((decision) => {
      const requirement = requirementsById.get(decision.unmatchedRequirementId);
      const fact = approvedCustomFactsById.get(decision.customFactId ?? "");

      return requirement && fact?.unmatchedRequirementId === requirement.id ? { requirement, fact } : null;
    })
    .filter((item): item is { requirement: UnmatchedRequirement; fact: CustomFact } => item !== null);
}

function customFactsForRequirement(facts: CustomFact[], unmatchedRequirementId: string): CustomFact[] {
  return facts.filter((fact) => fact.unmatchedRequirementId === unmatchedRequirementId);
}

function gapDecisionForRequirement(decisions: GapDecision[], unmatchedRequirementId: string): GapDecision | undefined {
  return decisions.find((decision) => decision.unmatchedRequirementId === unmatchedRequirementId);
}

function gapDecisionLabel(decision: GapDecisionValue): string {
  switch (decision) {
    case "Ignore":
      return "Ignored gap";
    case "MentionAsLearningInterest":
      return "Learning-interest gap";
    case "CoveredByCustomFact":
      return "Covered by custom fact";
    default:
      return decision;
  }
}

function gapDecisionTone(decision: GapDecision): string {
  if (decision.decision === "Ignore") {
    return "neutral";
  }

  return decision.decision === "CoveredByCustomFact" ? "approved" : "pending";
}

function gapDecisionClass(decision: GapDecision): string {
  switch (decision.decision) {
    case "Ignore":
      return "ignored";
    case "CoveredByCustomFact":
      return "covered";
    default:
      return "learning";
  }
}

function gapDecisionSummary(decision: GapDecision, customFacts: CustomFact[]): string {
  switch (decision.decision) {
    case "Ignore":
      return "Will not be called out in the review guidance.";
    case "CoveredByCustomFact": {
      const customFact = customFacts.find((fact) => fact.id === decision.customFactId);
      return customFact ? `Supported by approved job-local fact: ${customFact.title}.` : "Requires an approved job-local custom fact.";
    }
    default:
      return "May be framed cautiously as interest, not proof.";
  }
}

function emptyCustomFactDraft(): CustomFactDraft {
  return {
    title: "",
    summary: "",
    technologies: "",
    allowedClaims: ""
  };
}

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n|,/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function guidedActionButtonLabel(label: string, kind: string, workflowBusy: string | null): string {
  if (workflowBusy === "prepare" && kind === "prepare-application") {
    return "Preparing...";
  }

  if (workflowBusy === "review" && kind === "review-evidence") {
    return "Saving...";
  }

  if ((workflowBusy === "draft-edit" || workflowBusy === "audit") && kind === "refresh-audit") {
    return workflowBusy === "draft-edit" ? "Saving..." : "Auditing...";
  }

  if (workflowBusy === "draft" && kind === "generate-draft") {
    return "Generating...";
  }

  return label;
}

function preparationStatusLabel(status: string): string {
  switch (status) {
    case "PreparedForEvidenceReview":
      return "Prepared for evidence review";
    case "Preparing":
      return "Preparing";
    case "FailedProviderUnavailable":
      return "Preparation blocked";
    case "FailedInvalidProviderOutput":
      return "Preparation output blocked";
    case "PartiallyPreparedAnalysisOnly":
      return "Analysis prepared";
    default:
      return "Preparation not started";
  }
}

function preparationStatusTone(status: string): string {
  switch (status) {
    case "PreparedForEvidenceReview":
      return "approved";
    case "FailedProviderUnavailable":
    case "FailedInvalidProviderOutput":
      return "rejected";
    case "Preparing":
    case "PartiallyPreparedAnalysisOnly":
      return "pending";
    default:
      return "neutral";
  }
}

function profileFactStatusLabel(status: string): string {
  switch (status) {
    case "Draft":
      return "Draft evidence";
    case "Approved":
      return "Approved evidence";
    case "Archived":
      return "Archived evidence";
    case "Rejected":
      return "Rejected evidence";
    default:
      return status;
  }
}

function customFactStatusLabel(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "Pending confirmation";
    case "Approved":
      return "Approved custom fact";
    case "Rejected":
      return "Rejected custom fact";
    default:
      return status;
  }
}

function customFactStatusClass(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "pending";
    case "Approved":
      return "approved";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function customFactStatusTone(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "pending";
    case "Approved":
      return "approved";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function statusTone(status: string): string {
  switch (status) {
    case "Draft":
      return "draft";
    case "Approved":
      return "approved";
    case "Archived":
      return "archived";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function applicationHistoryEmptyMessage(includeArchived: boolean): string {
  return includeArchived
    ? "No application sessions yet."
    : "No active application sessions yet. Create one here, or include archived sessions to review older work.";
}

function summarizeClaimAudit(audit: ClaimAudit) {
  return audit.claims.reduce(
    (summary, claim) => {
      if (claim.status === "Supported") {
        return { ...summary, supported: summary.supported + 1 };
      }

      if (claim.status === "Unsupported") {
        return { ...summary, unsupported: summary.unsupported + 1 };
      }

      if (claim.status === "NeedsReview") {
        return { ...summary, needsReview: summary.needsReview + 1 };
      }

      return summary;
    },
    { supported: 0, unsupported: 0, needsReview: 0 }
  );
}

function parseJobSignals(value: string | undefined): JobSignalsDocument {
  return {
    provider: "Fake",
    extractedAt: "",
    requiredSkills: [],
    preferredSkills: [],
    responsibilities: [],
    signals: [],
    ...(parseJsonObject<Partial<JobSignalsDocument>>(value) ?? {})
  };
}

function parseClaimAudit(value: string | undefined): ClaimAudit {
  return {
    claims: [],
    ...(parseJsonObject<Partial<ClaimAudit>>(value) ?? {})
  };
}

function parseJsonArray<T>(value: string | undefined): T[] {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function parseJsonObject<T>(value: string | undefined): T | null {
  if (!value) {
    return null;
  }

  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function pageTitle(view: View): string {
  switch (view) {
    case "profile":
      return "Profile";
    case "applications":
      return "Applications";
    case "settings":
      return "Settings";
    default:
      return "Application writing workspace";
  }
}

function titleCase(value: string): string {
  return `${value.slice(0, 1).toUpperCase()}${value.slice(1)}`;
}

function disabledTitle(isDisabled: boolean, reason: string | null | undefined): string | undefined {
  return isDisabled && reason ? reason : undefined;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric"
  }).format(new Date(value));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}

export default App;
