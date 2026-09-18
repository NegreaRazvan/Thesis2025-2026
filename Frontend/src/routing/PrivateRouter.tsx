import { Navigate, Outlet } from "react-router-dom";
import { useAuthContext } from "../auth/context/AuthContext";

const PrivateRouter = () => {
  const { user, loading } = useAuthContext();
  if (loading) return <div style={{ padding: "2rem", color: "#9ca3af" }}>Loading…</div>;
  return user ? <Outlet /> : <Navigate to="/login" replace />;
};

export default PrivateRouter;
