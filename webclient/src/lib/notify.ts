import { toast } from "sonner"; 
export const notify = { 
    success: (m: string) => toast.success(m), 
    error: (m: string) => toast.error(m, { duration: 7000 }),
    info: (m: string) => toast(m), 
};