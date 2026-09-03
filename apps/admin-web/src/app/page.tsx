import styles from "./page.module.css";

export default function Home() {
  return (
    <div className={styles.page}>
      <main className={styles.main}>
        <h1>Lajan&apos;m — Back-office</h1>
        <p>
          Placeholder screen. Reconciliation, fraud review, compliance and
          support tooling land here in later modules (see
          services/api/src/modules/compliance and docs/architecture.md).
        </p>
      </main>
    </div>
  );
}
