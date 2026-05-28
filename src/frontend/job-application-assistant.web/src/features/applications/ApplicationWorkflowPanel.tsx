import type { RefObject } from "react";
import { StatusBadge } from "../../components/StatusBadge";
import type { ApplicationStrategy } from "../../applicationStrategy";
import { hasApplicationStrategyContent } from "../../applicationStrategy";
import type { CandidateFitBrief } from "../../candidateFitBrief";
import type { AuditExportNotice, CoverLetterExportState } from "../../exportControls";
import { getDraftReadinessLabel, isFakeProvider } from "../../readiness";
import type { ActionState, GuidedNextAction } from "../../readiness";
import type {
  AiDiagnostics,
  AiProviderStatus,
  ApplicationSession,
  ClaimAudit,
  CustomFact,
  CustomFactDraft,
  DraftQualityCheck,
  EvidenceMatch,
  GapDecision,
  GapDecisionValue,
  GeneratedDraftForm,
  InlineFeedback,
  JobSignalsDocument,
  UnmatchedRequirement
} from "../shared/types";
import {
  ApplicationStrategySummary,
  ProviderReadinessSummary
} from "../shared/summaries";
import { DraftEditorSection } from "./DraftEditorSection";
import { EvidenceReviewSection } from "./EvidenceReviewSection";
import { PreparationSection } from "./PreparationSection";

type ApplicationWorkflowPanelProps = {
  selectedApplication: ApplicationSession | undefined;
  selectedApplicationId: string | null;
  hasGeneratedDraft: boolean;
  hasSavedApprovedEvidence: boolean;
  savedApprovedEvidence: EvidenceMatch[];
  savedApprovedCustomFactEvidenceCount: number;
  guidedNextAction: GuidedNextAction;
  jobAnalysisState: ActionState;
  evidenceMatchingState: ActionState;
  draftGenerationState: ActionState;
  coverLetterExportState: CoverLetterExportState;
  jobSignals: JobSignalsDocument;
  evidenceMatches: EvidenceMatch[];
  unmatchedRequirements: UnmatchedRequirement[];
  savedGapDecisions: GapDecision[];
  gapDecisionsDraft: GapDecision[];
  customFacts: CustomFact[];
  customFactDrafts: Record<string, CustomFactDraft>;
  expandedCustomFactRequirementId: string | null;
  savedApprovedCustomFactEvidence: Array<{ requirement: UnmatchedRequirement; fact: CustomFact }>;
  currentApprovedCustomFactEvidence: Array<{ requirement: UnmatchedRequirement; fact: CustomFact }>;
  candidateFitBrief: CandidateFitBrief | null;
  applicationStrategy: ApplicationStrategy | null;
  claimAudit: ClaimAudit;
  draftQualityCheck: DraftQualityCheck;
  approvedEvidenceDraft: EvidenceMatch[];
  approvedEvidenceDraftSummaryCount: number;
  approvedRecommendedEvidenceCount: number;
  recommendedEvidenceMatches: EvidenceMatch[];
  reviewedWeakMatchIds: string[];
  currentGapDecisionCount: number;
  auditSummary: { supported: number; unsupported: number; needsReview: number };
  effectiveAuditReadiness: string;
  effectiveAuditExportNotice: AuditExportNotice | { tone: "warning"; message: string } | null;
  isDraftQualityBlocked: boolean;
  hasUnsavedDraftEdits: boolean;
  isRealProviderUnavailable: boolean;
  canRefreshClaimAudit: boolean;
  generatedDraftForm: GeneratedDraftForm;
  preparationDetailsOpen: boolean;
  evidenceDetailsOpen: boolean;
  strategyDetailsOpen: boolean;
  preparationDetailsSummary: string;
  evidenceDetailsSummary: string;
  strategyDetailsSummary: string;
  showCompactGapDecisionReview: boolean;
  canRunPreparation: boolean;
  workflowBusy: string | null;
  workflowBusyReason: string | null;
  exportBusy: "txt" | "docx" | null;
  exportBusyReason: string | null;
  exportFeedback: InlineFeedback | null;
  aiStatus: AiProviderStatus | null;
  aiDiagnostics: AiDiagnostics | null;
  aiDiagnosticsLastRanAt: string | null;
  evidenceReviewRef: RefObject<HTMLDivElement | null>;
  draftReviewRef: RefObject<HTMLElement | null>;
  exportPanelRef: RefObject<HTMLElement | null>;
  onRunGuidedNextAction: () => void;
  onPrepareApplication: () => void;
  onAnalyzeJob: () => void;
  onMatchEvidence: () => void;
  onGenerateDraft: () => void;
  onApproveMatch: (match: EvidenceMatch) => void;
  onApproveRecommendedEvidence: () => void;
  onReviewWeakMatch: (matchId: string) => void;
  onRemoveApprovedEvidence: (matchId: string) => void;
  onSetEvidenceReviewEditing: (isEditing: boolean) => void;
  onDecideGap: (requirementId: string, decision: GapDecisionValue, customFactId?: string) => void;
  onToggleCustomFactEditor: (requirementId: string) => void;
  onUpdateCustomFactDraft: (requirementId: string, patch: Partial<CustomFactDraft>) => void;
  onCreateCustomFact: (requirementId: string) => void;
  onUpdateCustomFactStatus: (factId: string, status: "Approved" | "Rejected") => void;
  onResetEvidenceReview: () => void;
  onSaveApprovedEvidence: () => void;
  onDraftFormChange: (form: GeneratedDraftForm) => void;
  onSaveGeneratedDraft: () => void;
  onRefreshClaimAudit: () => void;
  onCopyCoverLetter: () => void;
  onDownloadCoverLetter: (format: "txt" | "docx") => void;
  onClearExportFeedback: () => void;
  approvedEvidenceCountLabel: (count: number) => string;
  auditReadinessLabel: (readiness: string) => string;
  auditReadinessTone: (readiness: string) => string;
  claimAuditMessage: (hasGeneratedDraft: boolean) => string;
  customFactStatusClass: (status: string) => string;
  customFactStatusLabel: (status: string) => string;
  customFactStatusTone: (status: string) => string;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
  emptyCustomFactDraft: () => CustomFactDraft;
  formatDate: (value: string) => string;
  gapDecisionClass: (decision: GapDecision) => string;
  gapDecisionForRequirement: (decisions: GapDecision[], unmatchedRequirementId: string) => GapDecision | undefined;
  gapDecisionLabel: (decision: GapDecisionValue) => string;
  gapDecisionSummary: (decision: GapDecision, customFacts: CustomFact[]) => string;
  gapDecisionTone: (decision: GapDecision) => string;
  guidedActionButtonLabel: (label: string, kind: string, workflowBusy: string | null) => string;
  guidedStepLabel: (kind: string) => string;
  preparationStatusLabel: (status: string) => string;
  preparationStatusTone: (status: string) => string;
  customFactsForRequirement: (facts: CustomFact[], unmatchedRequirementId: string) => CustomFact[];
};

export function ApplicationWorkflowPanel(props: ApplicationWorkflowPanelProps) {
  return (
    <section className={`workflow-panel${props.hasGeneratedDraft ? " final-review-first" : ""}`}>
      <div className="section-heading">
        <h3>Cover letter workflow</h3>
        <p>Step 1 prepares the posting automatically. Step 2 only needs review when evidence or gaps require a decision. Step 3 generates and audits the draft.</p>
      </div>
      {props.aiStatus && (
        <ProviderReadinessSummary
          status={props.aiStatus}
          diagnostics={props.aiDiagnostics}
          diagnosticsLastRanAt={props.aiDiagnosticsLastRanAt}
        />
      )}
      <section className={`guided-action ${props.guidedNextAction.tone}`} aria-label="Guided next action">
        <div>
          <span>{props.guidedStepLabel(props.guidedNextAction.kind)}</span>
          <h4>{props.guidedNextAction.title}</h4>
          <p>{props.guidedNextAction.message}</p>
        </div>
        <button
          className="primary-action"
          type={props.guidedNextAction.kind === "save-posting" ? "submit" : "button"}
          onClick={props.guidedNextAction.kind === "save-posting" ? undefined : props.onRunGuidedNextAction}
          disabled={!props.guidedNextAction.canRun || props.workflowBusy !== null}
          title={props.disabledTitle(!props.guidedNextAction.canRun || props.workflowBusy !== null, props.workflowBusyReason ?? props.guidedNextAction.message)}
        >
          {props.guidedActionButtonLabel(props.guidedNextAction.buttonLabel, props.guidedNextAction.kind, props.workflowBusy)}
        </button>
      </section>
      {props.selectedApplication && (
        <div className="trust-chain" aria-label="Draft trust chain">
          <StatusBadge tone={props.preparationStatusTone(props.selectedApplication.preparationStatus)}>
            {props.preparationStatusLabel(props.selectedApplication.preparationStatus)}
          </StatusBadge>
          <StatusBadge tone={props.hasSavedApprovedEvidence ? "approved" : "pending"}>
            {props.approvedEvidenceCountLabel(props.savedApprovedEvidence.length + props.savedApprovedCustomFactEvidenceCount)}
          </StatusBadge>
          <StatusBadge tone={props.hasGeneratedDraft ? "approved" : "draft"}>
            {props.hasGeneratedDraft ? "Draft saved" : "No draft"}
          </StatusBadge>
          <StatusBadge tone={props.auditReadinessTone(props.effectiveAuditReadiness)}>
            {props.auditReadinessLabel(props.effectiveAuditReadiness)}
          </StatusBadge>
          <StatusBadge tone={props.coverLetterExportState.canExport ? "approved" : "pending"}>
            {props.coverLetterExportState.canExport ? "Export ready" : "Export blocked"}
          </StatusBadge>
        </div>
      )}
      {props.selectedApplication && (
        <PreparationSection
          selectedApplication={props.selectedApplication}
          open={props.preparationDetailsOpen}
          summary={props.preparationDetailsSummary}
          canRunPreparation={props.canRunPreparation}
          workflowBusy={props.workflowBusy}
          workflowBusyReason={props.workflowBusyReason}
          jobAnalysisState={props.jobAnalysisState}
          jobSignals={props.jobSignals}
          candidateFitBrief={props.candidateFitBrief}
          onPrepareApplication={props.onPrepareApplication}
          onAnalyzeJob={props.onAnalyzeJob}
          disabledTitle={props.disabledTitle}
        />
      )}

      {props.selectedApplication && (
        <EvidenceReviewSection
          open={props.evidenceDetailsOpen}
          summary={props.evidenceDetailsSummary}
          evidenceReviewRef={props.evidenceReviewRef}
          evidenceMatchingState={props.evidenceMatchingState}
          workflowBusy={props.workflowBusy}
          workflowBusyReason={props.workflowBusyReason}
          selectedApplicationId={props.selectedApplicationId}
          evidenceMatches={props.evidenceMatches}
          recommendedEvidenceMatches={props.recommendedEvidenceMatches}
          approvedEvidenceDraft={props.approvedEvidenceDraft}
          approvedRecommendedEvidenceCount={props.approvedRecommendedEvidenceCount}
          approvedEvidenceDraftSummaryCount={props.approvedEvidenceDraftSummaryCount}
          reviewedWeakMatchIds={props.reviewedWeakMatchIds}
          unmatchedRequirements={props.unmatchedRequirements}
          savedGapDecisions={props.savedGapDecisions}
          gapDecisionsDraft={props.gapDecisionsDraft}
          showCompactGapDecisionReview={props.showCompactGapDecisionReview}
          customFacts={props.customFacts}
          customFactDrafts={props.customFactDrafts}
          expandedCustomFactRequirementId={props.expandedCustomFactRequirementId}
          currentApprovedCustomFactEvidence={props.currentApprovedCustomFactEvidence}
          onMatchEvidence={props.onMatchEvidence}
          onApproveMatch={props.onApproveMatch}
          onApproveRecommendedEvidence={props.onApproveRecommendedEvidence}
          onReviewWeakMatch={props.onReviewWeakMatch}
          onRemoveApprovedEvidence={props.onRemoveApprovedEvidence}
          onSetEvidenceReviewEditing={props.onSetEvidenceReviewEditing}
          onDecideGap={props.onDecideGap}
          onToggleCustomFactEditor={props.onToggleCustomFactEditor}
          onUpdateCustomFactDraft={props.onUpdateCustomFactDraft}
          onCreateCustomFact={props.onCreateCustomFact}
          onUpdateCustomFactStatus={props.onUpdateCustomFactStatus}
          onResetEvidenceReview={props.onResetEvidenceReview}
          onSaveApprovedEvidence={props.onSaveApprovedEvidence}
          approvedEvidenceCountLabel={props.approvedEvidenceCountLabel}
          customFactStatusClass={props.customFactStatusClass}
          customFactStatusLabel={props.customFactStatusLabel}
          customFactStatusTone={props.customFactStatusTone}
          disabledTitle={props.disabledTitle}
          emptyCustomFactDraft={props.emptyCustomFactDraft}
          customFactsForRequirement={props.customFactsForRequirement}
          gapDecisionForRequirement={props.gapDecisionForRequirement}
          gapDecisionClass={props.gapDecisionClass}
          gapDecisionLabel={props.gapDecisionLabel}
          gapDecisionSummary={props.gapDecisionSummary}
          gapDecisionTone={props.gapDecisionTone}
        />
      )}

      <div className="workflow-step generated-draft-action">
        <div>
          <h4>Step 3. Generate and audit</h4>
          <p>{props.draftGenerationState.message}</p>
          {props.aiStatus && (
            <span className={`inline-readiness ${props.aiStatus.isAvailable ? "available" : "unavailable"} ${isFakeProvider(props.aiStatus) ? "fake" : ""}`}>
              {getDraftReadinessLabel(props.aiStatus)}
            </span>
          )}
        </div>
        <button
          className="secondary-workflow-action"
          type="button"
          onClick={props.onGenerateDraft}
          disabled={!props.draftGenerationState.canRun || props.workflowBusy !== null}
          title={props.disabledTitle(!props.draftGenerationState.canRun || props.workflowBusy !== null, props.workflowBusyReason ?? props.draftGenerationState.message)}
        >
          {props.workflowBusy === "draft" ? "Generating..." : props.hasGeneratedDraft ? "Regenerate and audit draft" : "Generate and audit draft"}
        </button>
      </div>

      {props.selectedApplication && hasApplicationStrategyContent(props.applicationStrategy) && (
        <details className="context-disclosure preparation-context" open={props.strategyDetailsOpen}>
          <summary>
            <span>Strategy details</span>
            <small>{props.strategyDetailsSummary}</small>
          </summary>
          <ApplicationStrategySummary strategy={props.applicationStrategy} />
        </details>
      )}

      {props.selectedApplication && (
        <DraftEditorSection
          selectedApplication={props.selectedApplication}
          generatedDraftForm={props.generatedDraftForm}
          hasUnsavedDraftEdits={props.hasUnsavedDraftEdits}
          effectiveAuditReadiness={props.effectiveAuditReadiness}
          effectiveAuditExportNotice={props.effectiveAuditExportNotice}
          isDraftQualityBlocked={props.isDraftQualityBlocked}
          draftQualityCheck={props.draftQualityCheck}
          claimAudit={props.claimAudit}
          auditSummary={props.auditSummary}
          coverLetterExportState={props.coverLetterExportState}
          exportBusy={props.exportBusy}
          exportBusyReason={props.exportBusyReason}
          exportFeedback={props.exportFeedback}
          workflowBusy={props.workflowBusy}
          workflowBusyReason={props.workflowBusyReason}
          canRefreshClaimAudit={props.canRefreshClaimAudit}
          isRealProviderUnavailable={props.isRealProviderUnavailable}
          aiStatus={props.aiStatus}
          draftReviewRef={props.draftReviewRef}
          exportPanelRef={props.exportPanelRef}
          onDraftFormChange={props.onDraftFormChange}
          onSaveGeneratedDraft={props.onSaveGeneratedDraft}
          onRefreshClaimAudit={props.onRefreshClaimAudit}
          onCopyCoverLetter={props.onCopyCoverLetter}
          onDownloadCoverLetter={props.onDownloadCoverLetter}
          onClearExportFeedback={props.onClearExportFeedback}
          formatDate={props.formatDate}
          disabledTitle={props.disabledTitle}
          claimAuditMessage={props.claimAuditMessage}
        />
      )}
    </section>
  );
}
