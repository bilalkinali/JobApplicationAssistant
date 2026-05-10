import "./styles.css";

const navigationItems = ["Home", "Profile", "Applications", "Settings"];

const workbenchCards = [
  {
    title: "Profile readiness",
    detail: "Contact details, tone preferences, and profile facts will appear here."
  },
  {
    title: "New application",
    detail: "A job posting entry point will live here when the workflow slice is added."
  },
  {
    title: "Recent applications",
    detail: "Saved application sessions will be listed here in a later issue."
  },
  {
    title: "AI status",
    detail: "Provider status and diagnostics will be added after the AI foundation exists."
  }
];

function App() {
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
          {navigationItems.map((item) => (
            <a href={`#${item.toLowerCase()}`} key={item}>
              {item}
            </a>
          ))}
        </nav>
      </aside>

      <section className="workspace" aria-labelledby="workspace-title">
        <header className="workspace-header">
          <div>
            <p className="eyebrow">Workbench</p>
            <h2 id="workspace-title">Application writing workspace</h2>
          </div>
          <span className="status-pill">Shell only</span>
        </header>

        <div className="panel-grid">
          {workbenchCards.map((card) => (
            <article className="panel" key={card.title}>
              <h3>{card.title}</h3>
              <p>{card.detail}</p>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

export default App;
