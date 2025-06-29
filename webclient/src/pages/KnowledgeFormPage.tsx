import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";

export default function KnowledgeFormPage() {
  const [files, setFiles] = useState<File[]>([]);
  const [name,  setName]  = useState("");
  const [pct,   setPct]   = useState(0);          // upload %
  const [busy,  setBusy]  = useState(false);
  const navigate = useNavigate();

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (e.target.files) setFiles(Array.from(e.target.files));
  }

  /* ---------- Upload with progress via XMLHttpRequest ---------- */
  async function onSave() {
    const form = new FormData();
    form.append("name", name);
    files.forEach(f => form.append("files", f));

    setBusy(true);
    setPct(0);

    await new Promise<void>((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open("POST", "/api/knowledge", true);

      xhr.upload.onprogress = e => {
        if (e.lengthComputable) setPct(Math.round((e.loaded / e.total) * 100));
      };

      xhr.onload = () => {
        setBusy(false);
        if (xhr.status === 201) {
          toast.success("Uploaded ✓");
          navigate("/knowledge");
          resolve();
        } else {
          toast.error(`Upload failed ${xhr.status}`);
          reject();
        }
      };

      xhr.onerror = () => {
        setBusy(false);
        toast.error("Network error");
        reject();
      };

      xhr.send(form);
    });
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

      {/* progress bar – only visible while busy */}
      {busy && <Progress value={pct} className="h-2" />}

      <Button
        onClick={onSave}
        disabled={busy || name.trim() === "" || files.length === 0}>
        {busy ? `Uploading, Please Wait... ${pct}%` : "Save"}
      </Button>
    </section>
  );
}
