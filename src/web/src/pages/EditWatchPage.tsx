import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getWatch, updateWatch, uploadWatchImages } from '../api/watches';
import WatchForm from '../components/WatchForm';
import type { Watch, CreateWatch } from '../types';

export default function EditWatchPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [watch, setWatch] = useState<Watch | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    getWatch(Number(id))
      .then(setWatch)
      .catch(() => navigate('/'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  async function handleSubmit(data: CreateWatch, files: File[]) {
    if (!id) return;
    await updateWatch(Number(id), data);
    if (files.length > 0) {
      await uploadWatchImages(Number(id), files);
    }
    navigate(`/watches/${id}`);
  }

  if (loading) return <p>Loading…</p>;
  if (!watch) return <p>Watch not found.</p>;

  return (
    <div>
      <h1>Edit {watch.brand} {watch.model}</h1>
      <WatchForm initial={watch} onSubmit={handleSubmit} submitLabel="Update Watch" />
    </div>
  );
}
