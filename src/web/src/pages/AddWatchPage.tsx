import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { createWatch, uploadWatchImages, getWatches } from '../api/watches';
import WatchForm from '../components/WatchForm';
import type { CreateWatch } from '../types';

export default function AddWatchPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [brands, setBrands] = useState<string[]>([]);

  const initialBrand = searchParams.get('brand') ?? '';
  const initialModel = searchParams.get('model') ?? '';

  useEffect(() => {
    getWatches().then((watches) => {
      const unique = [...new Set(watches.map((w) => w.brand))].sort();
      setBrands(unique);
    }).catch(() => {});
  }, []);

  async function handleSubmit(data: CreateWatch, files: File[]) {
    try {
      const watch = await createWatch(data);
      if (files.length > 0) {
        await uploadWatchImages(watch.id, files);
      }
      navigate(`/watches/${watch.id}`);
    } catch (error) {
      console.error('Failed to create watch:', error);
      alert('Failed to create watch. Please try again.');
    }
  }

  return (
    <div className="watch-form-page">
      <h1>Add Watch</h1>
      <WatchForm
        initial={initialBrand ? { brand: initialBrand, model: initialModel } : undefined}
        onSubmit={handleSubmit}
        submitLabel="Add Watch"
        onCancel={() => navigate('/')}
        brands={brands}
      />
    </div>
  );
}
