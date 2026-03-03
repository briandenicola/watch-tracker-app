import { useNavigate } from 'react-router-dom';
import { createWatch, uploadWatchImages } from '../api/watches';
import WatchForm from '../components/WatchForm';
import type { CreateWatch } from '../types';

export default function AddWatchPage() {
  const navigate = useNavigate();

  async function handleSubmit(data: CreateWatch, files: File[]) {
    const watch = await createWatch(data);
    if (files.length > 0) {
      await uploadWatchImages(watch.id, files);
    }
    navigate(`/watches/${watch.id}`);
  }

  return (
    <div>
      <h1>Add Watch</h1>
      <WatchForm onSubmit={handleSubmit} submitLabel="Add Watch" />
    </div>
  );
}
