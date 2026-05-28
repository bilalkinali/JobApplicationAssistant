import type { RefObject } from "react";
import { Field, Textarea } from "../../components/FormControls";
import { StatusBadge } from "../../components/StatusBadge";
import {
  evidenceQualityPresentation,
  isWeakEvidence,
  weakEvidenceReviewLabel
} from "../../evidenceReview";
import type { ActionState } from "../../readiness";
import type {
  CustomFact,
  CustomFactDraft,
  EvidenceMatch,
  GapDecision,
  GapDecisionValue,
  UnmatchedRequirement
} from "../shared/types";

type EvidenceReviewSectionProps = {
  open: boolean;
  summary: string;
  evidenceReviewRef: RefObject<HTMLDivElement | null>;
  evidenceMatchingState: ActionState;
  workflowBusy: string | null;
  workflowBusyReason: string | null;
  selectedApplicationId: string | null;
  evidenceMatches: EvidenceMatch[];
  recommendedEvidenceMatches: EvidenceMatch[];
  approvedEvidenceDraft: EvidenceMatch[];
  approvedRecommendedEvidenceCount: number;
  approvedEvidenceDraftSummaryCount: number;
  reviewedWeakMatchIds: string[];
  unmatchedRequirements: UnmatchedRequirement[];
  savedGapDecisions: GapDecision[];
  gapDecisionsDraft: GapDecision[];
  showCompactGapDecisionReview: boolean;
  customFacts: CustomFact[];
  customFactDrafts: Record<string, CustomFactDraft>;
  expandedCustomFactRequirementId: string | null;
  currentApprovedCustomFactEvidence: Array<{ requirement: UnmatchedRequirement; fact: CustomFact }>;
  onMatchEvidence: () => void;
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
  approvedEvidenceCountLabel: (count: number) => string;
  customFactStatusClass: (status: string) => string;
  customFactStatusLabel: (status: string) => string;
  customFactStatusTone: (status: string) => string;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
  emptyCustomFactDraft: () => CustomFactDraft;
  customFactsForRequirement: (facts: CustomFact[], unmatchedRequirementId: string) => CustomFact[];
  gapDecisionForRequirement: (decisions: GapDecision[], unmatchedRequirementId: string) => GapDecision | undefined;
  gapDecisionClass: (decision: GapDecision) => string;
  gapDecisionLabel: (decision: GapDecisionValue) => string;
  gapDecisionSummary: (decision: GapDecision, customFacts: CustomFact[]) => string;
  gapDecisionTone: (decision: GapDecision) => string;
};

export function EvidenceReviewSection(props: EvidenceReviewSectionProps) {
  return (
    <details className="context-disclosure preparation-context" open={props.open}>
      <summary>
        <span>Step 2. Review evidence</span>
        <small>{props.summary}</small>
      </summary>
      <div className="workflow-step">
        <div>
          <h4>Manual: evidence matching only</h4>
          <p>{props.evidenceMatchingState.message}</p>
        </div>
        <button
          className="secondary-workflow-action"
          type="button"
          onClick={props.onMatchEvidence}
          disabled={!props.evidenceMatchingState.canRun || props.workflowBusy !== null}
          title={props.disabledTitle(!props.evidenceMatchingState.canRun || props.workflowBusy !== null, props.workflowBusyReason ?? props.evidenceMatchingState.message)}
        >
          {props.workflowBusy === "matching" ? "Matching..." : "Match evidence"}
        </button>
      </div>

      <div className="review-grid" ref={props.evidenceReviewRef}>
        <section className="review-column">
          <div className="review-column-heading">
            <h4>Matched evidence</h4>
            <StatusBadge tone={props.approvedEvidenceDraftSummaryCount > 0 ? "approved" : "neutral"}>
              {props.approvedEvidenceCountLabel(props.approvedEvidenceDraftSummaryCount)}
            </StatusBadge>
          </div>
          <div className="evidence-toolbar">
            <button
              type="button"
              onClick={props.onApproveRecommendedEvidence}
              disabled={props.recommendedEvidenceMatches.length === 0 || props.approvedRecommendedEvidenceCount === props.recommendedEvidenceMatches.length}
              title={props.disabledTitle(
                props.recommendedEvidenceMatches.length === 0 || props.approvedRecommendedEvidenceCount === props.recommendedEvidenceMatches.length,
                props.recommendedEvidenceMatches.length === 0
                  ? "No strong or partial evidence matches are available yet."
                  : "All recommended evidence is already selected."
              )}
            >
              Approve suggested evidence
            </button>
            <small>
              {props.approvedRecommendedEvidenceCount}/{props.recommendedEvidenceMatches.length} strong or partial matches selected
            </small>
          </div>
          {props.evidenceMatches.length === 0 && <p className="empty-state compact">No matches yet.</p>}
          {props.evidenceMatches.map((match) => {
            const quality = evidenceQualityPresentation(match.quality);
            const isWeakMatch = isWeakEvidence(match);
            const hasReviewedWeakMatch = props.reviewedWeakMatchIds.includes(match.id);
            const isApproved = props.approvedEvidenceDraft.some((item) => item.id === match.id);

            return (
              <article className={`evidence-card ${quality.cardClass} ${isApproved ? "approved" : ""}`} key={match.id}>
                <div className="evidence-card-heading">
                  <strong>{match.signal}</strong>
                  <StatusBadge tone={isApproved ? "approved" : quality.tone}>
                    {isApproved ? "Approved evidence" : quality.label}
                  </StatusBadge>
                </div>
                <span>{match.profileFactTitle}</span>
                <p>{match.summary}</p>
                {match.reason && <small>Reason: {match.reason}</small>}
                <small>{quality.guidance}</small>
                {match.matchedTerms.length > 0 && <small>Matched: {match.matchedTerms.join(", ")}</small>}
                {isWeakMatch ? (
                  <div className="weak-match-review">
                    <button
                      type="button"
                      className={hasReviewedWeakMatch || isApproved ? "selected" : ""}
                      onClick={() => props.onReviewWeakMatch(match.id)}
                      aria-pressed={hasReviewedWeakMatch || isApproved}
                    >
                      {weakEvidenceReviewLabel(hasReviewedWeakMatch || isApproved)}
                    </button>
                    {(hasReviewedWeakMatch || isApproved) && (
                      <button type="button" onClick={() => (isApproved ? props.onRemoveApprovedEvidence(match.id) : props.onApproveMatch(match))}>
                        {isApproved ? "Remove from approved" : "Approve weak match"}
                      </button>
                    )}
                    <small>Weak matches should only guide cautious wording or prompt stronger evidence.</small>
                  </div>
                ) : (
                  <button type="button" onClick={() => (isApproved ? props.onRemoveApprovedEvidence(match.id) : props.onApproveMatch(match))}>
                    {isApproved ? "Remove from approved" : "Approve"}
                  </button>
                )}
              </article>
            );
          })}
        </section>

        <section className="review-column">
          <div className="review-column-heading">
            <h4>Unmatched requirements</h4>
            {props.showCompactGapDecisionReview && (
              <button type="button" onClick={() => props.onSetEvidenceReviewEditing(true)}>
                Edit evidence review
              </button>
            )}
          </div>
          {props.unmatchedRequirements.length === 0 && <p className="empty-state compact">No unmatched requirements recorded.</p>}
          {props.showCompactGapDecisionReview ? (
            <div className="gap-decision-list compact-review">
              {props.savedGapDecisions.map((decision) => {
                const requirement = props.unmatchedRequirements.find((item) => item.id === decision.unmatchedRequirementId);

                return (
                  <article className={`gap-decision-item ${props.gapDecisionClass(decision)}`} key={decision.unmatchedRequirementId}>
                    <div>
                      <StatusBadge tone={props.gapDecisionTone(decision)}>
                        {props.gapDecisionLabel(decision.decision)}
                      </StatusBadge>
                      <strong>{requirement?.requirement ?? decision.unmatchedRequirementId}</strong>
                      <span>{props.gapDecisionSummary(decision, props.customFacts)}</span>
                    </div>
                  </article>
                );
              })}
            </div>
          ) : (
            props.unmatchedRequirements.map((requirement) => {
              const gapDecision = props.gapDecisionForRequirement(props.gapDecisionsDraft, requirement.id);
              const requirementCustomFacts = props.customFactsForRequirement(props.customFacts, requirement.id);
              const approvedCustomFacts = requirementCustomFacts.filter((fact) => fact.status === "Approved");
              const customFactDraft = props.customFactDrafts[requirement.id] ?? props.emptyCustomFactDraft();
              const isCustomFactEditorExpanded = props.expandedCustomFactRequirementId === requirement.id;

              return (
                <article className="evidence-card muted" key={requirement.id}>
                  <div className="gap-card-heading">
                    <strong>{requirement.requirement}</strong>
                    {gapDecision ? (
                      <StatusBadge tone={gapDecision.decision === "Ignore" ? "neutral" : "pending"}>
                        {props.gapDecisionLabel(gapDecision.decision)}
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
                      onClick={() => props.onDecideGap(requirement.id, "Ignore")}
                    >
                      Ignore
                    </button>
                    <button
                      type="button"
                      className={gapDecision?.decision === "MentionAsLearningInterest" ? "selected" : ""}
                      onClick={() => props.onDecideGap(requirement.id, "MentionAsLearningInterest")}
                    >
                      Mention as learning interest
                    </button>
                    {approvedCustomFacts.map((fact) => (
                      <button
                        type="button"
                        className={gapDecision?.decision === "CoveredByCustomFact" && gapDecision.customFactId === fact.id ? "selected" : ""}
                        key={fact.id}
                        onClick={() => props.onDecideGap(requirement.id, "CoveredByCustomFact", fact.id)}
                      >
                        Cover with {fact.title}
                      </button>
                    ))}
                    <button
                      type="button"
                      className={isCustomFactEditorExpanded ? "selected" : ""}
                      onClick={() => props.onToggleCustomFactEditor(requirement.id)}
                      aria-expanded={isCustomFactEditorExpanded}
                    >
                      Add custom fact
                    </button>
                  </div>

                  {gapDecision && gapDecision.decision !== "CoveredByCustomFact" && (
                    <article className={`gap-decision-item compact-inline ${props.gapDecisionClass(gapDecision)}`}>
                      <div>
                        <StatusBadge tone={props.gapDecisionTone(gapDecision)}>
                          {props.gapDecisionLabel(gapDecision.decision)}
                        </StatusBadge>
                        <span>{props.gapDecisionSummary(gapDecision, props.customFacts)}</span>
                      </div>
                    </article>
                  )}

                  {isCustomFactEditorExpanded && (
                    <div className="custom-fact-editor">
                      <Field
                        label="Custom fact title"
                        value={customFactDraft.title}
                        onChange={(title) => props.onUpdateCustomFactDraft(requirement.id, { title })}
                      />
                      <Textarea
                        label="Custom fact summary"
                        value={customFactDraft.summary}
                        onChange={(summary) => props.onUpdateCustomFactDraft(requirement.id, { summary })}
                      />
                      <Textarea
                        label="Technologies"
                        value={customFactDraft.technologies}
                        onChange={(technologies) => props.onUpdateCustomFactDraft(requirement.id, { technologies })}
                      />
                      <Textarea
                        label="Allowed claims"
                        value={customFactDraft.allowedClaims}
                        onChange={(allowedClaims) => props.onUpdateCustomFactDraft(requirement.id, { allowedClaims })}
                      />
                      <button
                        type="button"
                        onClick={() => props.onCreateCustomFact(requirement.id)}
                        disabled={props.workflowBusy !== null}
                        title={props.disabledTitle(props.workflowBusy !== null, props.workflowBusyReason)}
                      >
                        {props.workflowBusy === `custom-fact-${requirement.id}` ? "Adding..." : "Add job-local fact"}
                      </button>
                    </div>
                  )}

                  {requirementCustomFacts.length > 0 && (
                    <div className="custom-fact-list compact">
                      {requirementCustomFacts.map((fact) => (
                        <article className={`custom-fact ${props.customFactStatusClass(fact.status)}`} key={fact.id}>
                          <StatusBadge tone={props.customFactStatusTone(fact.status)}>{props.customFactStatusLabel(fact.status)}</StatusBadge>
                          <strong>{fact.title}</strong>
                          <p>{fact.summary}</p>
                          {fact.technologies && fact.technologies.length > 0 && <small>{fact.technologies.join(", ")}</small>}
                          {fact.status === "PendingConfirmation" && (
                            <div className="custom-fact-actions">
                              <button
                                type="button"
                                onClick={() => props.onUpdateCustomFactStatus(fact.id, "Approved")}
                                disabled={props.workflowBusy !== null}
                                title={props.disabledTitle(props.workflowBusy !== null, props.workflowBusyReason)}
                              >
                                Approve
                              </button>
                              <button
                                type="button"
                                onClick={() => props.onUpdateCustomFactStatus(fact.id, "Rejected")}
                                disabled={props.workflowBusy !== null}
                                title={props.disabledTitle(props.workflowBusy !== null, props.workflowBusyReason)}
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
            })
          )}
        </section>
      </div>

      <div className="workflow-step">
        <div>
          <h4>Save evidence decisions</h4>
          <p>Save the selected evidence and any required gap decisions before draft generation.</p>
        </div>
        <div className="workflow-action-group">
          <button
            className="secondary-workflow-action"
            type="button"
            onClick={props.onResetEvidenceReview}
            disabled={!props.selectedApplicationId || props.workflowBusy !== null}
            title={props.disabledTitle(
              !props.selectedApplicationId || props.workflowBusy !== null,
              props.workflowBusyReason ?? "Save the application before resetting evidence review."
            )}
          >
            {props.workflowBusy === "review" ? "Resetting..." : "Reset evidence"}
          </button>
          <button
            className="secondary-workflow-action"
            type="button"
            onClick={props.onSaveApprovedEvidence}
            disabled={!props.selectedApplicationId || props.workflowBusy !== null}
            title={props.disabledTitle(
              !props.selectedApplicationId || props.workflowBusy !== null,
              props.workflowBusyReason ?? "Save the application before reviewing evidence."
            )}
          >
            {props.workflowBusy === "review" ? "Saving..." : "Save evidence review"}
          </button>
        </div>
      </div>

      <div className="approved-list">
        {props.approvedEvidenceDraft.length === 0 && props.currentApprovedCustomFactEvidence.length === 0 && (
          <p className="empty-state compact">No approved evidence selected.</p>
        )}
        {props.approvedEvidenceDraft.map((match) => (
          <article className="approved-item" key={match.id}>
            <div>
              <StatusBadge tone="approved">Approved profile evidence</StatusBadge>
              <strong>{match.signal}</strong>
              <span>{match.profileFactTitle}</span>
            </div>
            <button type="button" onClick={() => props.onRemoveApprovedEvidence(match.id)}>Remove</button>
          </article>
        ))}
        {props.currentApprovedCustomFactEvidence.map((item) => (
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
        {props.gapDecisionsDraft.length === 0 && <p className="empty-state compact">No gap decisions selected.</p>}
        {props.gapDecisionsDraft.map((decision) => {
          const requirement = props.unmatchedRequirements.find((item) => item.id === decision.unmatchedRequirementId);

          return (
            <article className={`gap-decision-item ${props.gapDecisionClass(decision)}`} key={decision.unmatchedRequirementId}>
              <div>
                <StatusBadge tone={props.gapDecisionTone(decision)}>
                  {props.gapDecisionLabel(decision.decision)}
                </StatusBadge>
                <strong>{requirement?.requirement ?? decision.unmatchedRequirementId}</strong>
                <span>{props.gapDecisionSummary(decision, props.customFacts)}</span>
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
        {props.customFacts.length === 0 ? (
          <p className="empty-state compact">No job-local custom facts recorded.</p>
        ) : (
          <div className="custom-fact-list">
            {props.customFacts.map((fact) => (
              <article className={`custom-fact ${props.customFactStatusClass(fact.status)}`} key={fact.id}>
                <StatusBadge tone={props.customFactStatusTone(fact.status)}>{props.customFactStatusLabel(fact.status)}</StatusBadge>
                <strong>{fact.title}</strong>
                <p>{fact.summary}</p>
                {fact.technologies && fact.technologies.length > 0 && <small>{fact.technologies.join(", ")}</small>}
              </article>
            ))}
          </div>
        )}
      </section>
    </details>
  );
}
