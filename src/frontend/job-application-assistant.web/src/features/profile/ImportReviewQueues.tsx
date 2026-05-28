import type { RefObject } from "react";
import type { ImportedDraftFactReviewQueue, ProfileFact } from "../shared/types";

type ImportReviewQueuesProps = {
  queues: ImportedDraftFactReviewQueue[];
  selectedImportedDraftFactIds: string[];
  containerRef: RefObject<HTMLDivElement | null>;
  selectedIdsForQueue: (queue: ImportedDraftFactReviewQueue) => string[];
  duplicateScopeLabel: (scope: string) => string;
  onToggleSelection: (factId: string) => void;
  onOpenFact: (fact: ProfileFact) => void;
  onMerge: (queue: ImportedDraftFactReviewQueue) => void;
  onBulkApprove: (queue: ImportedDraftFactReviewQueue) => void;
  onBulkArchive: (queue: ImportedDraftFactReviewQueue) => void;
};

export function ImportReviewQueues(props: ImportReviewQueuesProps) {
  if (props.queues.length === 0) {
    return null;
  }

  return (
    <div className="import-review-queues" ref={props.containerRef}>
      {props.queues.map((queue) => (
        <section className="import-review-queue" key={queue.importSessionId}>
          <div className="section-heading">
            <h4>{queue.fileName}</h4>
            <p>{queue.draftFactCount} imported draft facts grouped for review.</p>
          </div>
          <div className="import-review-actions">
            <button type="button" disabled={props.selectedIdsForQueue(queue).length < 2} onClick={() => props.onMerge(queue)}>
              Merge selected
            </button>
            <button type="button" disabled={props.selectedIdsForQueue(queue).length === 0} onClick={() => props.onBulkApprove(queue)}>
              Approve selected
            </button>
            <button type="button" disabled={props.selectedIdsForQueue(queue).length === 0} onClick={() => props.onBulkArchive(queue)}>
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
                  <article className={`import-review-item${item.hasDuplicateIndicators ? " duplicate" : ""}`} key={item.profileFact.id}>
                    <label className="import-review-select">
                      <input
                        type="checkbox"
                        checked={props.selectedImportedDraftFactIds.includes(item.profileFact.id)}
                        onChange={() => props.onToggleSelection(item.profileFact.id)}
                      />
                      <span>Select</span>
                    </label>
                    <button type="button" onClick={() => props.onOpenFact(item.profileFact)}>
                      <span className="import-review-title">{item.profileFact.title}</span>
                      <span className="import-review-context">{item.sourceContext}</span>
                      {item.hasDuplicateIndicators && (
                        <span className="duplicate-indicators">
                          {item.duplicateIndicators.map((indicator) => (
                            <small key={`${indicator.scope}-${indicator.profileFactId}`}>
                              {props.duplicateScopeLabel(indicator.scope)}: {indicator.profileFactTitle} - {indicator.reason}
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
  );
}
