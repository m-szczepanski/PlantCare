import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { PlantPhoto } from "@/components/PlantPhoto";
import { Button } from "@/components/ui/button";
import { touchButton } from "@/lib/ui";

const ACCEPTED_TYPES = ["image/jpeg", "image/png", "image/webp", "image/gif"];
export const PHOTO_MAX_BYTES = 5 * 1024 * 1024;

export type PhotoFileErrorKey = "form.photoBadType" | "form.photoTooLarge";

export function photoFileError(file: File): PhotoFileErrorKey | null {
  if (!ACCEPTED_TYPES.includes(file.type)) return "form.photoBadType";
  if (file.size > PHOTO_MAX_BYTES) return "form.photoTooLarge";
  return null;
}

export interface PhotoPickerProps {
  currentUrl: string | null;
  nickName: string;
  value: File | null;
  onChange: (file: File | null) => void;
}

export function PhotoPicker({ currentUrl, nickName, value, onChange }: PhotoPickerProps) {
  const { t } = useTranslation();
  const inputRef = useRef<HTMLInputElement>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!value) {
      setPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(value);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [value]);

  function handleFile(file: File | null) {
    setError(null);
    if (!file) {
      onChange(null);
      return;
    }
    const errorKey = photoFileError(file);
    if (errorKey) {
      setError(t(errorKey, { max: PHOTO_MAX_BYTES / 1024 / 1024 }));
      return;
    }
    onChange(file);
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-3">
        <PlantPhoto photoUrl={previewUrl ?? currentUrl} nickName={nickName} className="h-20 w-20 shrink-0" />
        <div className="space-y-1">
          <div className="flex flex-wrap gap-2">
            <input
              ref={inputRef}
              type="file"
              accept={ACCEPTED_TYPES.join(",")}
              className="hidden"
              onChange={(event) => {
                handleFile(event.target.files?.[0] ?? null);
                event.target.value = "";
              }}
            />
            <Button
              type="button"
              variant="secondary"
              onClick={() => inputRef.current?.click()}
              className={touchButton}
            >
              {currentUrl || previewUrl ? t("form.changePhoto") : t("form.choosePhoto")}
            </Button>
            {value ? (
              <Button type="button" variant="ghost" onClick={() => onChange(null)} className={touchButton}>
                {t("form.removePhoto")}
              </Button>
            ) : null}
          </div>
          <p className="text-xs text-muted-foreground">{t("form.photoHint")}</p>
          {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
        </div>
      </div>
    </div>
  );
}
