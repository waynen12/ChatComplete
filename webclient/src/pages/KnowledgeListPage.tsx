import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogTrigger,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogCancel,
  AlertDialogAction,
} from "@/components/ui/alert-dialog";

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

  async function handleDelete(id: string) {
    const res = await fetch(`/api/knowledge/${id}`, { method: "DELETE" });

    if (res.ok) {
      // optimistic UI update
      setCollections(prev => prev.filter(c => c.id !== id));
      toast.success(`Collection ${id} Deleted ✓`);
    } else {
      toast.error(`Failed (${res.status})`);
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
                <th />
              </tr>
            </thead>
            <tbody>
              {collections.map((c) => (
                <tr key={c.id} className="border-b">
                  <td className="py-2">{c.name}</td>
                  <td>{c.documentCount}</td>
                  <td className="text-right space-x-2">
                    <Button asChild size="sm" variant="outline">
                      <Link to={`/knowledge/${c.id}/edit`}>Edit</Link>
                    </Button>
                    <Button asChild size="sm">
                      <Link to={`/chat/${c.id}`}>Chat</Link>
                    </Button>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button>Delete</Button>
                      </AlertDialogTrigger>

                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Delete “{c.name}”?</AlertDialogTitle>
                          <AlertDialogDescription>
                            This will remove the collection and its search index permanently.
                            You can’t undo this action.
                          </AlertDialogDescription>
                        </AlertDialogHeader>

                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>

                          <AlertDialogAction
                            onClick={() => handleDelete(c.id)}>
                            Yes, delete
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>

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
