// Production: set VITE_JOB_MATCH_URL on Render (Environment → Build) to your Job Match web
// service URL, e.g. https://job-match-xxxx.onrender.com — avoids nginx proxy issues and 429s.
const jobMatchHref =
  import.meta.env.VITE_JOB_MATCH_URL?.trim().replace(/\/$/, '') || '';

const services = [
  {
    name: 'שירות התאמת משרות',
    description:
      'ניתוח הזדמנויות עבודה ביחס לפרופיל המקצועי שלך באמצעות בינה מלאכותית.',
    path: jobMatchHref ? `${jobMatchHref}/` : '/job-match/',
  },
  {
    name: 'מעקב מועמדויות',
    description: 'מעקב אחר מועמדויות לעבודה, ראיונות ועדכוני סטטוס.',
    path: 'https://application-tracker-latest-b8l9.onrender.com/',
  },
];

export default function App() {
  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>פלטפורמת חיפוש עבודה</h1>
        <p style={styles.subtitle}>ערכת כלים לחיפוש עבודה מבוססת בינה מלאכותית — גרסה 0.1.0</p>
      </header>

      <main style={styles.grid}>
        {services.map((svc) => (
          <a
            key={svc.name}
            href={svc.path}
            style={styles.card}
            onMouseEnter={(e) => (e.currentTarget.style.transform = 'translateY(-4px)')}
            onMouseLeave={(e) => (e.currentTarget.style.transform = 'translateY(0)')}
          >
            <h2 style={styles.cardTitle}>{svc.name}</h2>
            <p style={styles.cardDesc}>{svc.description}</p>
            <span style={styles.cardLink}>פתיחה &larr;</span>
          </a>
        ))}
      </main>

      <footer style={styles.footer}>
        <p>&copy; 2026 פלטפורמת חיפוש עבודה</p>
      </footer>
    </div>
  );
}

const styles = {
  container: {
    minHeight: '100vh',
    direction: 'rtl',
    fontFamily:
      "'Segoe UI', 'Rubik', 'Noto Sans Hebrew', 'Arial Hebrew', system-ui, -apple-system, sans-serif",
    background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
    color: '#e2e8f0',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: '2rem',
  },
  header: {
    textAlign: 'center',
    marginBottom: '3rem',
    marginTop: '4rem',
  },
  title: {
    fontSize: '2.5rem',
    fontWeight: 700,
    margin: 0,
    background: 'linear-gradient(90deg, #38bdf8, #818cf8)',
    WebkitBackgroundClip: 'text',
    WebkitTextFillColor: 'transparent',
  },
  subtitle: {
    fontSize: '1.1rem',
    color: '#94a3b8',
    marginTop: '0.5rem',
  },
  grid: {
    display: 'flex',
    gap: '2rem',
    flexWrap: 'wrap',
    justifyContent: 'center',
    maxWidth: '800px',
  },
  card: {
    background: '#1e293b',
    border: '1px solid #334155',
    borderRadius: '12px',
    padding: '2rem',
    width: '340px',
    textDecoration: 'none',
    color: '#e2e8f0',
    transition: 'transform 0.2s, box-shadow 0.2s',
    boxShadow: '0 4px 24px rgba(0,0,0,0.2)',
    cursor: 'pointer',
  },
  cardTitle: {
    fontSize: '1.3rem',
    fontWeight: 600,
    marginTop: 0,
    color: '#f1f5f9',
  },
  cardDesc: {
    color: '#94a3b8',
    lineHeight: 1.6,
  },
  cardLink: {
    color: '#38bdf8',
    fontWeight: 500,
  },
  footer: {
    marginTop: 'auto',
    paddingTop: '3rem',
    color: '#475569',
    fontSize: '0.9rem',
  },
};
