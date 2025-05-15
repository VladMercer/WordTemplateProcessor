'use client';

import React, { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';

export default function Home() {
  const [file, setFile] = useState<File | null>(null);
  const [fields, setFields] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);

  const { control, handleSubmit, setValue } = useForm<Record<string, string>>();

  const parseTemplate = async (file: File) => {
    setIsLoading(true);
    setPdfUrl(null);
    setFields([]);

    try {
      const fd = new FormData();
      fd.append('file', file);

      const res = await fetch('http://localhost:5002/Template/parse-template', {
        method: 'POST',
        body: fd,
      });
      if (!res.ok) throw new Error(`Status ${res.status}`);

      const names: string[] = await res.json();
      setFields(names);
      names.forEach(name => setValue(name, ''));
    } catch (err) {
      console.error('Ошибка парсинга шаблона:', err);
      alert('Не удалось распарсить шаблон.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0] ?? null;
    if (!selected) return;
    setFile(selected);
    parseTemplate(selected);
  };

  const handleGeneratePdf = async (data: Record<string, string>) => {
    if (!file) return;
    setIsLoading(true);

    try {
      const fd = new FormData();
      fd.append('Template', file);
      fd.append('Fields', JSON.stringify(data));

      const res = await fetch('http://localhost:5002/Template/fill-template', {
        method: 'POST',
        body: fd,
      });
      if (!res.ok) throw new Error(`Status ${res.status}`);

      const blob = await res.blob();
      setPdfUrl(URL.createObjectURL(blob));
    } catch (err) {
      console.error('Ошибка генерации PDF:', err);
      alert('Не удалось сгенерировать PDF.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-dark text-light min-vh-100 d-flex align-items-start pt-5">
      <div className="container">
        <h1 className="mb-4">Заполнение Word‑шаблона</h1>

        <div className="mb-4">
          <label className="form-label">Выберите .docx файл:</label>
          <input
            type="file"
            accept=".docx"
            onChange={handleFileChange}
            className="form-control bg-dark text-light border-light"
            disabled={isLoading}
          />
        </div>

        {isLoading && <p>Загружаем и парсим шаблон…</p>}

        {fields.length > 0 && (
          <form onSubmit={handleSubmit(handleGeneratePdf)} className="mb-4">
            <div className="row">
              {fields.map((name, index) => (
                <div className="col-md-6 mb-3" key={name}>
                  <label htmlFor={name} className="form-label">{name}:</label>
                  <Controller
                    name={name}
                    control={control}
                    render={({ field }) => (
                      <input
                        id={name}
                        {...field}
                        className="form-control bg-dark text-light border-secondary"
                        placeholder={`Введите значение для ${name}`}
                      />
                    )}
                  />
                </div>
              ))}
            </div>

            <button type="submit" className="btn btn-outline-light w-100" disabled={isLoading}>
              {isLoading ? 'Генерируем PDF…' : 'Получить PDF'}
            </button>
          </form>
        )}

        {pdfUrl && (
          <div className="alert alert-success">
            <a href={pdfUrl} download="filled-template.pdf" className="btn btn-success w-100">
              Скачать готовый PDF
            </a>
          </div>
        )}
      </div>
    </div>
  );
}