import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Loader2 } from "lucide-react";

/** allowed extensions w/o dot (lower-case) */
const ALLOWED_EXT = ["pdf", "docx", "md", "txt"];
const MAX_MB = 100;

export default function KnowledgeFormPage() {
  const [files, setFiles] = useState<File[]>([]);
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (!e.target.files) return;

    const accepted: File[] = [];
    for (const f of Array.from(e.target.files)) {
      const ext = f.name.split(".").pop()?.toLowerCase();
      if (!ext || !ALLOWED_EXT.includes(ext)) {
        toast.error(`❌ ${f.name} – unsupported type`);
        continue;
      }
      if (f.size > MAX_MB * 1024 * 1024) {
        toast.error(`❌ ${f.name} – larger than ${MAX_MB} MB`);
        continue;
      }
      accepted.push(f);
    }
    setFiles(prev => [...prev, ...accepted]);
    // reset the <input> so selecting same file twice fires change event
    e.target.value = "";
  }

  /* ---------- Upload with progress via XMLHttpRequest ---------- */
  async function onSave() {
    setBusy(true);
    try {
      const form = new FormData();
      form.append("name", name);
      files.forEach(f => form.append("files", f));


      const res = await fetch("/api/knowledge", { method: "POST", body: form });

      if (res.ok) {
        toast.success("Uploaded ✓");
        navigate("/knowledge");
      } else {
        toast.error(`Upload failed (${res.status})`);
      }
    } catch (err) {
      toast.error("Upload failed – network error");
      console.error(err);
    } finally {
      setBusy(false);
    }
  }


  /* ---------- UI ---------- */
  return (
    <section className="max-w-xl mx-auto space-y-6 p-6">
      <input
        type="text"
        placeholder="Collection name"
        className="input w-full"
        value={name}
        onChange={e => setName(e.target.value)}
        disabled={busy}
      />

      <input
        type="file"
        multiple
        accept=".pdf,.docx,.md,.txt"
        onChange={onFileChange}
        className="block w-full border rounded-md p-2"
        disabled={busy}
      />

      <ul className="mt-2 space-y-1 text-sm">
        {files.map(f => (
          <li key={f.name} className="flex justify-between">
            <span>{f.name} ({(f.size / 1024).toFixed(0)} KB)</span>
            {!busy && (
              <button
                type="button"
                onClick={() => setFiles(files.filter(x => x !== f))}
                className="text-red-500">×</button>
            )}
          </li>
        ))}
      </ul>

      <Button
        onClick={onSave}
        disabled={busy || name.trim() === "" || files.length === 0}>
        {busy && <Loader2 className="mr-2 animate-spin" />}
        {busy ? "Uploading…" : "Save"}
      </Button>
    </section>
  );
}
