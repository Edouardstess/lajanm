'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { createFaq, deleteFaq, FaqEntry, getAllFaqs, updateFaq } from '../../lib/adminApi';

export default function FaqPage() {
  const [entries, setEntries] = useState<FaqEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const [category, setCategory] = useState('');
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState('');
  const [sortOrder, setSortOrder] = useState('0');

  const load = () => {
    setLoading(true);
    setError(null);
    getAllFaqs()
      .then(setEntries)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  // queueMicrotask: see the same note on the other list pages — keeps the
  // synchronous setState out of the effect body for react-hooks lint.
  useEffect(() => {
    queueMicrotask(load);
  }, []);

  const onCreate = async () => {
    if (!category.trim() || !question.trim() || !answer.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const created = await createFaq({
        category: category.trim(),
        question: question.trim(),
        answer: answer.trim(),
        sortOrder: Number(sortOrder) || 0,
      });
      setEntries((prev) => [...prev, created]);
      setCategory('');
      setQuestion('');
      setAnswer('');
      setSortOrder('0');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to create entry');
    } finally {
      setBusy(false);
    }
  };

  const togglePublished = async (entry: FaqEntry) => {
    setBusy(true);
    try {
      const updated = await updateFaq(entry.id, { isPublished: !entry.isPublished });
      setEntries((prev) => prev.map((e) => (e.id === entry.id ? updated : e)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update entry');
    } finally {
      setBusy(false);
    }
  };

  const onDelete = async (id: string) => {
    setBusy(true);
    try {
      await deleteFaq(id);
      setEntries((prev) => prev.filter((e) => e.id !== id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to delete entry');
    } finally {
      setBusy(false);
    }
  };

  return (
    <RequireAuth>
      <h1>FAQ content</h1>
      <p className="stat-label" style={{ marginBottom: 16 }}>
        Unpublished entries stay hidden from the mobile app until you publish them.
      </p>

      <div className="card" style={{ marginBottom: 24 }}>
        <label htmlFor="faq-category">Category</label>
        <input id="faq-category" value={category} onChange={(e) => setCategory(e.target.value)} />

        <label htmlFor="faq-question">Question</label>
        <input id="faq-question" value={question} onChange={(e) => setQuestion(e.target.value)} />

        <label htmlFor="faq-answer">Answer</label>
        <textarea id="faq-answer" rows={4} value={answer} onChange={(e) => setAnswer(e.target.value)} />

        <label htmlFor="faq-order">Sort order</label>
        <input id="faq-order" type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} />

        <button
          className="button"
          disabled={busy || !category.trim() || !question.trim() || !answer.trim()}
          onClick={onCreate}
        >
          Add entry
        </button>
      </div>

      <h2>Entries</h2>
      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && entries.length === 0 && <p className="stat-label">No FAQ entries yet.</p>}

      {entries.map((e) => (
        <div className="card" key={e.id}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
            <strong>{e.question}</strong>
            <span className="badge">{e.isPublished ? 'published' : 'draft'}</span>
          </div>
          <p style={{ marginBottom: 8, whiteSpace: 'pre-wrap' }}>{e.answer}</p>
          <p className="stat-label" style={{ marginBottom: 12 }}>
            {e.category} · order {e.sortOrder}
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="button button-secondary" disabled={busy} onClick={() => togglePublished(e)}>
              {e.isPublished ? 'Unpublish' : 'Publish'}
            </button>
            <button className="button button-danger" disabled={busy} onClick={() => onDelete(e.id)}>
              Delete
            </button>
          </div>
        </div>
      ))}
    </RequireAuth>
  );
}
