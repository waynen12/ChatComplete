import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { Trash2 } from "lucide-react";  

interface KnowledgeItem {
  id: string;
  name: string;
  documentCount: number;
  created: string; // ISO date
}

export default function KnowledgeListPage() {
  const [collections, setCollections] = useState<KnowledgeItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const res = await fetch("/api/knowledge");
        const data = await res.json();
        setCollections(data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function deleteCollection(id: string) {
    if (!confirm(`Delete '${id}'? This cannot be undone.`)) return;

    const res = await fetch(`/api/knowledge/${id}`, { method: "DELETE" });
    if (res.ok) {
      toast.success("Deleted ✓");
      setCollections(cols => cols.filter(c => c.id !== id));   // local update
    } else {
      const msg = await res.text();
      toast.error(`Delete failed (${res.status})`);
      console.error(msg);
    }
  }

  return (
    <section className="container py-8">
      <header className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold">Knowledge Collections</h2>
        <Button asChild>
          <Link to="/knowledge/new">New Collection</Link>
        </Button>
      </header>

      {loading && <p>Loading…</p>}

      {!loading && collections.length === 0 && (
        <p className="text-muted-foreground">No collections found.</p>
      )}

      {!loading && collections.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left py-2">Name</th>
                <th className="text-left py-2">Documents</th>
                <th className="text-left py-2">Created</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {collections.map((c) => (
                <tr key={c.id} className="border-b">
                  <td className="py-2">{c.name}</td>
                  <td>{c.documentCount}</td>
                  <td>{new Date(c.created).toLocaleDateString()}</td>
                  <td className="text-right space-x-2">
                    <Button asChild size="sm" variant="outline">
                      <Link to={`/knowledge/${c.id}/edit`}>Edit</Link>
                    </Button>
                    <Button asChild size="sm">
                      <Link to={`/chat/${c.id}`}>Chat</Link>
                    </Button>
                    <Button
                      size="icon"
                      variant="destructive"
                      onClick={() => deleteCollection(c.id)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
