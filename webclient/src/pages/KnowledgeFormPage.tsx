import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import type {FormEvent} from "react";

export default function KnowledgeFormPage() {
  const { id } = useParams(); // undefined → create mode
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);

    const formData = new FormData();
    formData.append("name", name);
    formData.append("description", description);
    files.forEach((f) => formData.append("files", f));

    await fetch(isEdit ? `/api/knowledge/${id}` : "/api/knowledge", {
      method: isEdit ? "PUT" : "POST",
      body: formData,
    });

    navigate("/knowledge");
  }

  return (
    <section className="container py-8 max-w-2xl">
      <h2 className="text-2xl font-semibold mb-6">
        {isEdit ? "Edit Collection" : "New Collection"}
      </h2>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label className="block mb-2 font-medium">Name</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </div>

        <div>
          <label className="block mb-2 font-medium">Description</label>
          <Textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
          />
        </div>

        <div>
          <label className="block mb-2 font-medium">Upload Documents</label>
          <Input
            type="file"
            multiple
            onChange={(e) => setFiles(Array.from(e.target.files ?? []))}
          />
          {files.length > 0 && (
            <p className="mt-2 text-sm text-muted-foreground">
              {files.length} file(s) ready to upload
            </p>
          )}
        </div>

        <div className="flex gap-4">
          <Button type="submit" disabled={saving}>
            {saving ? "Saving…" : "Save"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/knowledge")}
          >
            Cancel
          </Button>
        </div>
      </form>
    </section>
  );
}
