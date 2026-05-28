import { StatusBadge } from "../../components/StatusBadge";
import type { ProfileFact } from "../shared/types";

type ProfileFactsPanelProps = {
  profileFacts: ProfileFact[];
  selectedProfileFactId: string | null;
  selectedProfileFactIds: string[];
  allProfileFactsSelected: boolean;
  statusTone: (status: string) => string;
  profileFactStatusLabel: (status: string) => string;
  onToggleAll: () => void;
  onToggleFact: (factId: string) => void;
  onOpenFact: (fact: ProfileFact) => void;
  onDeleteSelected: () => void;
};

export function ProfileFactsPanel(props: ProfileFactsPanelProps) {
  return (
    <>
      {props.profileFacts.length === 0 && <p className="empty-state">No profile facts yet.</p>}
      {props.profileFacts.length > 0 && (
        <div className="fact-bulk-actions">
          <label className="checkbox-field">
            <input type="checkbox" checked={props.allProfileFactsSelected} onChange={props.onToggleAll} />
            <span>Select all</span>
          </label>
          <button
            className="danger-action"
            type="button"
            disabled={props.selectedProfileFactIds.length === 0}
            onClick={props.onDeleteSelected}
          >
            Delete selected
          </button>
          {props.selectedProfileFactIds.length > 0 && <small>{props.selectedProfileFactIds.length} selected</small>}
        </div>
      )}
      <div className="fact-list">
        {props.profileFacts.map((fact) => (
          <article className={`fact-card ${fact.status.toLowerCase()}${fact.id === props.selectedProfileFactId ? " active" : ""}`} key={fact.id}>
            <label className="fact-select">
              <input
                type="checkbox"
                checked={props.selectedProfileFactIds.includes(fact.id)}
                onChange={() => props.onToggleFact(fact.id)}
              />
              <span>Select fact</span>
            </label>
            <button type="button" className="fact-open-button" onClick={() => props.onOpenFact(fact)}>
              <strong>{fact.title}</strong>
              <span>{fact.type}</span>
              <StatusBadge tone={props.statusTone(fact.status)}>{props.profileFactStatusLabel(fact.status)}</StatusBadge>
            </button>
          </article>
        ))}
      </div>
    </>
  );
}
