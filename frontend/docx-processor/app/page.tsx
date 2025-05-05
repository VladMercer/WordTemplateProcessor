'use client';

import React, { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import styles from './page.module.css';

type FieldInfo = {
  name: string;
  type: 0 | 1; // 0 - Text, 1 - Image
};

export default function Home() {
  const [file, setFile] = useState<File | null>(null);
  const [fields, setFields] = useState<FieldInfo[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);

  const { control, handleSubmit, setValue } = useForm();

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
        const data: { [key: string]: 0 | 1 } = await response.json();
        const fieldArray = Object.entries(data).map(([name, type]) => ({ name, type }));
        setFields(fieldArray);
        
        // Инициализируем значения формы
        fieldArray.forEach((field) => {
          setValue(field.name, field.type === 1 ? null : '');
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

      // Разделяем поля на текстовые и изображения
      const textFields: { [key: string]: string } = {};
      const imageFields: { [key: string]: File } = {};

      fields.forEach((field) => {
        const value = data[field.name];
        if (field.type === 1 && value instanceof File) {
          formData.append(field.name, value);
        } else if (field.type === 0) {
          textFields[field.name] = value;
        }
      });

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

      {fields.length > 0 && (
        <form onSubmit={handleSubmit(handleGeneratePdf)}>
          {fields.map((field) => (
            <div key={field.name}>
              <label htmlFor={field.name}>{field.name}:</label>
              {field.type === 1 ? (
                <Controller
                  name={field.name}
                  control={control}
                  render={({ field: { onChange } }) => (
                    <input
                      type="file"
                      accept="image/*"
                      onChange={(e) => onChange(e.target.files?.[0])}
                    />
                  )}
                />
              ) : (
                <Controller
                  name={field.name}
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
          <a href={pdfUrl} download="filled-template.pdf">
            Скачать PDF
          </a>
        </div>
      )}
    </div>
  );
}