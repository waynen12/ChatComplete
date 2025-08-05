import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import { ChevronUp, ChevronDown, Search } from "lucide-react";
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

type SortField = 'name' | 'documentCount' | 'created';
type SortDirection = 'asc' | 'desc';

export default function KnowledgeListPage() {
  const [collections, setCollections] = useState<KnowledgeItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortField, setSortField] = useState<SortField>('name');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

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

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };

  const filteredAndSortedCollections = collections
    .filter(collection => 
      collection.name.toLowerCase().includes(searchTerm.toLowerCase())
    )
    .sort((a, b) => {
      let aValue: string | number;
      let bValue: string | number;
      
      switch (sortField) {
        case 'name':
          aValue = a.name.toLowerCase();
          bValue = b.name.toLowerCase();
          break;
        case 'documentCount':
          aValue = a.documentCount;
          bValue = b.documentCount;
          break;
        case 'created':
          aValue = new Date(a.created).getTime();
          bValue = new Date(b.created).getTime();
          break;
        default:
          return 0;
      }
      
      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

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
        <div className="flex items-center gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
            <Input
              placeholder="Search collections..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 w-64"
            />
          </div>
          <Button asChild>
            <Link to="/knowledge/new">New Collection</Link>
          </Button>
        </div>
      </header>

      {loading && <p>Loading…</p>}

      {!loading && collections.length === 0 && (
        <p className="text-muted-foreground">No collections found.</p>
      )}

      {!loading && filteredAndSortedCollections.length === 0 && collections.length > 0 && (
        <p className="text-muted-foreground text-center">No collections match your search.</p>
      )}

      {!loading && collections.length > 0 && (
        <div className="flex justify-center">
          <div className="overflow-x-auto w-[70%]">
            <div className="rounded-lg border bg-card text-card-foreground shadow-sm">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-4 px-6 font-semibold w-1/2">
                      <button
                        onClick={() => handleSort('name')}
                        className="flex items-center gap-2 hover:text-primary transition-colors"
                      >
                        Name
                        {sortField === 'name' && (
                          sortDirection === 'asc' ? 
                          <ChevronUp className="h-4 w-4" /> : 
                          <ChevronDown className="h-4 w-4" />
                        )}
                      </button>
                    </th>
                    <th className="text-left py-4 px-6 font-semibold w-1/4">
                      <button
                        onClick={() => handleSort('documentCount')}
                        className="flex items-center gap-2 hover:text-primary transition-colors"
                      >
                        Documents
                        {sortField === 'documentCount' && (
                          sortDirection === 'asc' ? 
                          <ChevronUp className="h-4 w-4" /> : 
                          <ChevronDown className="h-4 w-4" />
                        )}
                      </button>
                    </th>
                    <th className="py-4 px-6 w-1/4">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredAndSortedCollections.map((c, index) => (
                    <tr key={c.id} className={`border-b last:border-b-0 hover:bg-muted/30 transition-colors ${index % 2 === 0 ? 'bg-background' : 'bg-muted/10'}`}>
                      <td className="py-4 px-6 font-medium w-1/2">{c.name}</td>
                      <td className="py-4 px-6 text-muted-foreground w-1/4">{c.documentCount}</td>
                      <td className="py-4 px-6 text-right space-x-2 w-1/4">
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
                            <AlertDialogTitle>Delete "{c.name}"?</AlertDialogTitle>
                            <AlertDialogDescription>
                              This will remove the collection and its search index permanently.
                              You can't undo this action.
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
          </div>
        </div>
      )}
    </section>
  );
}
