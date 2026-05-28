import type { RefObject } from "react";
import { Textarea } from "../../components/FormControls";
import { isFakeProvider } from "../../readiness";
import type { AuditExportNotice, CoverLetterExportState } from "../../exportControls";
import type {
  AiProviderStatus,
  ApplicationSession,
  ClaimAudit,
  DraftQualityCheck,
  GeneratedDraftForm,
  InlineFeedback
} from "../shared/types";
import { ExportPanel } from "./ExportPanel";

type DraftEditorSectionProps = {
  selectedApplication: ApplicationSession;
  generatedDraftForm: GeneratedDraftForm;
  hasUnsavedDraftEdits: boolean;
  effectiveAuditReadiness: string;
  effectiveAuditExportNotice: AuditExportNotice | { tone: "warning"; message: string } | null;
  isDraftQualityBlocked: boolean;
  draftQualityCheck: DraftQualityCheck;
  claimAudit: ClaimAudit;
  auditSummary: { supported: number; unsupported: number; needsReview: number };
  coverLetterExportState: CoverLetterExportState;
  exportBusy: "txt" | "docx" | null;
  exportBusyReason: string | null;
  exportFeedback: InlineFeedback | null;
  workflowBusy: string | null;
  workflowBusyReason: string | null;
  canRefreshClaimAudit: boolean;
  isRealProviderUnavailable: boolean;
  aiStatus: AiProviderStatus | null;
  draftReviewRef: RefObject<HTMLElement | null>;
  exportPanelRef: RefObject<HTMLElement | null>;
  onDraftFormChange: (form: GeneratedDraftForm) => void;
  onSaveGeneratedDraft: () => void;
  onRefreshClaimAudit: () => void;
  onCopyCoverLetter: () => void;
  onDownloadCoverLetter: (format: "txt" | "docx") => void;
  onClearExportFeedback: () => void;
  formatDate: (value: string) => string;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
  claimAuditMessage: (hasGeneratedDraft: boolean) => string;
};

export function DraftEditorSection(props: DraftEditorSectionProps) {
  const generatedDraft = props.selectedApplication.generatedDraft;

  if (!generatedDraft) {
    return (
      <ExportPanel
        coverLetterExportState={props.coverLetterExportState}
        exportBusy={props.exportBusy}
        exportBusyReason={props.exportBusyReason}
        exportFeedback={props.exportFeedback}
        effectiveAuditExportNotice={props.effectiveAuditExportNotice}
        disabled
        hasGeneratedDraft={false}
        onCopyCoverLetter={props.onCopyCoverLetter}
        onDownloadCoverLetter={props.onDownloadCoverLetter}
        disabledTitle={props.disabledTitle}
        claimAuditMessage={props.claimAuditMessage}
      />
    );
  }

  return (
    <section className="draft-editor" ref={props.draftReviewRef}>
      <div className="section-heading">
        <h4>Current draft</h4>
        <p>
          Generated {props.formatDate(generatedDraft.generatedAt)}
          {generatedDraft.lastEditedAt ? ` - Edited ${props.formatDate(generatedDraft.lastEditedAt)}` : ""}
          {props.effectiveAuditReadiness === "Stale" ? " - Audit stale" : ""}
        </p>
      </div>
      {props.aiStatus && isFakeProvider(props.aiStatus) && (
        <p className="workflow-note warning">Fake AI mode: this draft uses deterministic demo/test output.</p>
      )}
      {props.isDraftQualityBlocked && (
        <section className="claim-audit">
          <div className="section-heading">
            <h4>Draft quality</h4>
            <p>
              Needs revision - {props.draftQualityCheck.copiedSevenWordPhraseCount} copied seven-word phrases detected
              {props.draftQualityCheck.copiedPhraseThreshold > 0 ? `, threshold ${props.draftQualityCheck.copiedPhraseThreshold}` : ""}
            </p>
          </div>
          <p className="workflow-note warning">{props.coverLetterExportState.reason ?? "Resolve draft quality issues before exporting."}</p>
          <div className="audit-list">
            {props.draftQualityCheck.issues.map((issue) => (
              <article className="audit-item needsreview" key={issue.code}>
                <span>{issue.severity}</span>
                <p>{issue.message}</p>
              </article>
            ))}
          </div>
        </section>
      )}
      {props.effectiveAuditExportNotice && <p className={`workflow-note ${props.effectiveAuditExportNotice.tone}`}>{props.effectiveAuditExportNotice.message}</p>}
      <Textarea
        label="Cover letter"
        value={props.generatedDraftForm.coverLetterText}
        onChange={(coverLetterText) => {
          props.onDraftFormChange({ ...props.generatedDraftForm, coverLetterText });
          props.onClearExportFeedback();
        }}
      />
      <Textarea
        label="Short motivation"
        value={props.generatedDraftForm.shortMotivationText}
        onChange={(shortMotivationText) => {
          props.onDraftFormChange({ ...props.generatedDraftForm, shortMotivationText });
          props.onClearExportFeedback();
        }}
      />
      <div className="form-actions">
        <button
          className="secondary-workflow-action"
          type="button"
          onClick={props.onSaveGeneratedDraft}
          disabled={!props.hasUnsavedDraftEdits || props.workflowBusy !== null}
          title={props.disabledTitle(
            !props.hasUnsavedDraftEdits || props.workflowBusy !== null,
            props.workflowBusyReason ?? "Edit the draft before saving changes."
          )}
        >
          {props.workflowBusy === "draft-edit" ? "Saving..." : "Save draft edits"}
        </button>
        <button
          type="button"
          onClick={props.onRefreshClaimAudit}
          disabled={!props.canRefreshClaimAudit || props.workflowBusy !== null}
          title={props.disabledTitle(
            !props.canRefreshClaimAudit || props.workflowBusy !== null,
            props.workflowBusyReason ??
              (props.isRealProviderUnavailable
                ? "Open AI settings before refreshing claim audit."
                : "Claim audit is current.")
          )}
        >
          {props.workflowBusy === "audit" ? "Auditing..." : "Refresh claim audit"}
        </button>
      </div>
      <ExportPanel
        coverLetterExportState={props.coverLetterExportState}
        exportBusy={props.exportBusy}
        exportBusyReason={props.exportBusyReason}
        exportFeedback={props.exportFeedback}
        effectiveAuditExportNotice={props.effectiveAuditExportNotice}
        panelRef={props.exportPanelRef}
        onCopyCoverLetter={props.onCopyCoverLetter}
        onDownloadCoverLetter={props.onDownloadCoverLetter}
        disabledTitle={props.disabledTitle}
        claimAuditMessage={props.claimAuditMessage}
      />
      <section className={`claim-audit ${props.isDraftQualityBlocked ? "subordinate-to-quality" : ""}`}>
        <div className="section-heading">
          <h4>Claim audit</h4>
          <p>
            {props.isDraftQualityBlocked
              ? generatedDraft.auditUpdatedAt
                ? `Draft quality still blocks export - audit updated ${props.formatDate(generatedDraft.auditUpdatedAt)}`
                : "Draft quality still blocks export - run claim audit after the generated text is ready."
              : generatedDraft.auditUpdatedAt
                ? `Updated ${props.formatDate(generatedDraft.auditUpdatedAt)} - ${props.auditSummary.supported} supported, ${props.auditSummary.unsupported} unsupported, ${props.auditSummary.needsReview} needs review`
              : "Run claim audit after the generated text is ready."}
          </p>
        </div>
        {props.effectiveAuditExportNotice && <p className={`workflow-note ${props.effectiveAuditExportNotice.tone}`}>{props.effectiveAuditExportNotice.message}</p>}
        {props.isDraftQualityBlocked && generatedDraft.auditUpdatedAt && (
          <p className="audit-counts-subordinate">
            {props.auditSummary.supported} supported, {props.auditSummary.unsupported} unsupported, {props.auditSummary.needsReview} needs review
          </p>
        )}
        {props.claimAudit.claims.length === 0 ? (
          <p className="empty-state compact">No claim audit results yet.</p>
        ) : (
          <div className="audit-list">
            {props.claimAudit.claims.map((claim) => (
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
  );
}
