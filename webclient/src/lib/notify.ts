import { toast } from "sonner"; 
export const notify = { 
    success: (m: string) => toast.success(m), 
    error: (m: string) => toast.error(m, { duration: 7000 }),
    warning: (m: string) => toast.warning(m),
    info: (m: string) => toast(m), 
};