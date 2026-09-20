import { useMemo, useState } from "react";
import { BookOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PlantPhoto } from "@/components/PlantPhoto";
import { useJournalEntries, useJournalMutations } from "@/hooks/usePlants";
import { touchButton, touchField } from "@/lib/ui";
import { formatInstant } from "@/lib/dates";

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function JournalCard({ plantId, nickName }: { plantId: number; nickName: string }) {
  const { data: entries = [] } = useJournalEntries(plantId);
  const { add, remove } = useJournalMutations(plantId);
  const [date, setDate] = useState(today);
  const [text, setText] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [fromId, setFromId] = useState<number | null>(null);
  const [toId, setToId] = useState<number | null>(null);

  const oldest = entries.length > 0 ? entries[entries.length - 1] : null;
  const newest = entries.length > 0 ? entries[0] : null;
  const from = entries.find((entry) => entry.id === fromId) ?? oldest;
  const to = entries.find((entry) => entry.id === toId) ?? newest;

  const canCompare = entries.length >= 2 && from && to && from.id !== to.id;

  const sortedForSelect = useMemo(() => [...entries].reverse(), [entries]);

  function submit() {
    add.mutate(
      {
        entryDate: date ? `${date}T00:00:00` : undefined,
        text: text.trim() === "" ? undefined : text.trim(),
        file,
      },
      {
        onSuccess: () => {
          setText("");
          setFile(null);
        },
      },
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <BookOpen className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          Care journal
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap items-end gap-2">
          <div className="space-y-1">
            <Label htmlFor="journalDate">Date</Label>
            <Input id="journalDate" type="date" value={date} onChange={(e) => setDate(e.target.value)} className={touchField} />
          </div>
          <div className="min-w-40 flex-1 space-y-1">
            <Label htmlFor="journalText">Note</Label>
            <Input
              id="journalText"
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="New leaf unfurling..."
              className={touchField}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="journalFile">Photo</Label>
            <Input
              id="journalFile"
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className={`${touchField} file:mr-2 file:border-0 file:bg-muted file:px-2 file:py-1 file:text-xs`}
            />
          </div>
          <Button
            className={touchButton}
            disabled={add.isPending || (text.trim() === "" && !file)}
            onClick={submit}
          >
            {add.isPending ? "Adding..." : "Add entry"}
          </Button>
        </div>

        {canCompare && from && to ? (
          <div>
            <p className="mb-2 text-sm font-medium">Before / after</p>
            <div className="grid grid-cols-2 gap-4">
              {[from, to].map((entry, index) => (
                <figure key={entry.id} className="space-y-1">
                  <PlantPhoto
                    photoUrl={entry.photoUrl}
                    nickName={nickName}
                    className="aspect-square w-full"
                  />
                  <figcaption className="text-xs text-muted-foreground">
                    {index === 0 ? "Before" : "After"} ·{" "}
                    {formatInstant(entry.entryDate, false)}
                    {entry.text ? ` — ${entry.text}` : ""}
                  </figcaption>
                </figure>
              ))}
            </div>
          </div>
        ) : null}

        {entries.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No journal entries yet — log a photo or a note as your plant grows.
          </p>
        ) : (
          <ul className="space-y-2 text-sm">
            {entries.map((entry) => (
              <li key={entry.id} className="flex items-center gap-3 rounded-md border p-2">
                <PlantPhoto photoUrl={entry.photoUrl} nickName={nickName} className="h-10 w-10 shrink-0" />
                <div className="min-w-0 flex-1">
                  <span className="font-medium">
                    {formatInstant(entry.entryDate, false)}
                  </span>
                  {entry.text ? (
                    <span className="ml-2 text-muted-foreground">{entry.text}</span>
                  ) : null}
                </div>
                <Button
                  size="sm"
                  variant="ghost"
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(entry.id)}
                >
                  Delete
                </Button>
              </li>
            ))}
          </ul>
        )}

        {entries.length >= 2 ? (
          <div className="flex flex-wrap items-center gap-2 text-sm">
            <span className="text-muted-foreground">Compare:</span>
            <select
              aria-label="Before entry"
              value={String(from?.id ?? "")}
              onChange={(e) => setFromId(Number(e.target.value))}
              className="h-9 rounded-md border border-input bg-transparent px-2"
            >
              {sortedForSelect.map((entry) => (
                <option key={entry.id} value={String(entry.id)}>
                  {formatInstant(entry.entryDate, false)}
                </option>
              ))}
            </select>
            <span>vs</span>
            <select
              aria-label="After entry"
              value={String(to?.id ?? "")}
              onChange={(e) => setToId(Number(e.target.value))}
              className="h-9 rounded-md border border-input bg-transparent px-2"
            >
              {sortedForSelect.map((entry) => (
                <option key={entry.id} value={String(entry.id)}>
                  {formatInstant(entry.entryDate, false)}
                </option>
              ))}
            </select>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}

export default JournalCard;
