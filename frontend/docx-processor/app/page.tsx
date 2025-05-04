'use client';

import React, { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import styles from './page.module.css';

export default function Home() {
  const [file, setFile] = useState<File | null>(null);
  const [fields, setFields] = useState<{ [key: string]: string }>({});
  const [isLoading, setIsLoading] = useState(false);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);

  const { control, handleSubmit, setValue, getValues } = useForm();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setFile(e.target.files[0]);
    }
  };

  const handleFileUpload = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      alert('Пожалуйста, загрузите файл.');
      return;
    }

    try {
      setIsLoading(true);

      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('http://localhost:5002/Template/parse-template', {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        const data = await response.json();
        setFields(data);
        Object.keys(data).forEach((key) => {
          setValue(key, key.endsWith(':img') ? null : '');
        });
      } else {
        console.error('Ошибка при получении данных для полей.');
      }
    } catch (error) {
      console.error('Ошибка при загрузке файла:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleGeneratePdf = async (data: any) => {
    try {
      setIsLoading(true);

      const formData = new FormData();
      formData.append('Template', file!);

      // Отправляем файлы изображений и собираем остальные поля
      const textFields: { [key: string]: string } = {};

      for (const key of Object.keys(data)) {
        const value = data[key];
        if (key.endsWith(':img') && value instanceof File) {
          formData.append(key, value);
        } else {
          textFields[key] = value;
        }
      }

      formData.append('Fields', JSON.stringify(textFields));

      const response = await fetch('http://localhost:5002/Template/fill-template', {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        setPdfUrl(url);
      } else {
        console.error('Ошибка при генерации PDF.');
      }
    } catch (error) {
      console.error('Ошибка при отправке данных на сервер для генерации PDF:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.page}>
      <h1>Загрузка шаблона и заполнение полей</h1>

      <form onSubmit={handleFileUpload}>
        <div>
          <label htmlFor="fileInput">Выберите файл:</label>
          <input type="file" id="fileInput" onChange={handleFileChange} required />
        </div>
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Загрузка...' : 'Загрузить файл'}
        </button>
      </form>

      {Object.keys(fields).length > 0 && (
        <form onSubmit={handleSubmit(handleGeneratePdf)}>
          {Object.keys(fields).map((field) => (
            <div key={field}>
              <label htmlFor={field}>{field}:</label>
              {field.endsWith(':img') ? (
                <input
                  type="file"
                  accept="image/*"
                  onChange={(e) => {
                    if (e.target.files?.[0]) {
                      setValue(field, e.target.files[0]);
                    }
                  }}
                />
              ) : (
                <Controller
                  name={field}
                  control={control}
                  render={({ field }) => <input {...field} />}
                />
              )}
            </div>
          ))}
          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Отправка...' : 'Получить PDF'}
          </button>
        </form>
      )}

      {pdfUrl && (
        <div>
          <a href={pdfUrl} download="filled-template.pdf">Скачать PDF</a>
        </div>
      )}
    </div>
  );
}