import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuthContext } from "../context/AuthContext";
import useAuthApi from "../AuthApi";
import "../auth.css";

function useFormState() {
    const [loading, setLoading] = useState(false);
    const [error,   setError]   = useState("");

    const submit = async (fn: () => Promise<void>) => {
        setLoading(true); setError("");
        try   { await fn(); }
        catch { setError("err"); }
        finally { setLoading(false); }
    };

    return { loading, error, setError, submit };
}

const AuthPage = () => {
    const { t } = useTranslation();
    const [isSignUp, setIsSignUp] = useState(false);

    const [loginEmail, setLoginEmail] = useState("");
    const [loginPass,  setLoginPass]  = useState("");
    const loginForm                   = useFormState();

    const [regEmail,    setRegEmail]    = useState("");
    const [regUsername, setRegUsername] = useState("");
    const [regPass,     setRegPass]     = useState("");
    const regForm                       = useFormState();

    const { login }                     = useAuthContext();
    const { login: apiLogin, register } = useAuthApi();
    const navigate                      = useNavigate();

    const handleLogin = (e: React.FormEvent) => {
        e.preventDefault();
        loginForm.submit(async () => {
            const data = await apiLogin(loginEmail, loginPass);
            login({ token: data.token, username: data.username, userId: data.userId, email: data.email });
            navigate("/");
        });
        loginForm.setError(loginForm.error || "");
    };

    const handleRegister = (e: React.FormEvent) => {
        e.preventDefault();
        regForm.submit(async () => {
            const data = await register(regEmail, regUsername, regPass);
            login({ token: data.token, username: data.username, userId: data.userId, email: data.email });
            navigate("/");
        });
    };

    return (
        <div className="auth-root">
            <div className={`auth-container ${isSignUp ? "active" : ""}`}>

                <div className="auth-form-container auth-sign-in">
                    <form onSubmit={handleLogin}>
                        <h1>{t("auth.signIn")}</h1>
                        <span>{t("auth.useEmail")}</span>
                        <input
                            type="email" placeholder={t("auth.email")} required
                            value={loginEmail} onChange={e => setLoginEmail(e.target.value)}
                        />
                        <input
                            type="password" placeholder={t("auth.password")} required
                            value={loginPass} onChange={e => setLoginPass(e.target.value)}
                        />
                        {loginForm.error && <p className="auth-error">{t("auth.invalidCredentials")}</p>}
                        <button type="submit" disabled={loginForm.loading}>
                            {loginForm.loading ? t("auth.signingIn") : t("auth.signIn")}
                        </button>
                        <p className="auth-mobile-switch">
                            {t("auth.noAccount")}{" "}
                            <a href="#" onClick={e => { e.preventDefault(); setIsSignUp(true); }}>{t("auth.signUpLink")}</a>
                        </p>
                    </form>
                </div>

                <div className="auth-form-container auth-sign-up">
                    <form onSubmit={handleRegister}>
                        <h1>{t("auth.createAccount")}</h1>
                        <span>{t("auth.useEmailReg")}</span>
                        <input
                            type="text" placeholder={t("auth.username")} required minLength={3}
                            value={regUsername} onChange={e => setRegUsername(e.target.value)}
                        />
                        <input
                            type="email" placeholder={t("auth.email")} required
                            value={regEmail} onChange={e => setRegEmail(e.target.value)}
                        />
                        <input
                            type="password" placeholder={t("auth.password")} required minLength={8}
                            value={regPass} onChange={e => setRegPass(e.target.value)}
                        />
                        {regForm.error && <p className="auth-error">{t("auth.emailTaken")}</p>}
                        <button type="submit" disabled={regForm.loading}>
                            {regForm.loading ? t("auth.creating") : t("auth.signUp")}
                        </button>
                    </form>
                </div>

                <div className="auth-toggle-container">
                    <div className="auth-toggle">
                        <div className="auth-toggle-panel auth-toggle-left">
                            <div className="auth-logo-hero"><div className="auth-logo-mark">LF</div> LinguaForge</div>
                            <h1>{t("auth.welcomeBack")}</h1>
                            <p>{t("auth.alreadyAccount")}</p>
                            <button className="auth-toggle-btn" onClick={() => setIsSignUp(false)} type="button">
                                {t("auth.signIn")}
                            </button>
                        </div>
                        <div className="auth-toggle-panel auth-toggle-right">
                            <div className="auth-logo-hero"><div className="auth-logo-mark">LF</div> LinguaForge</div>
                            <h1>{t("auth.helloFriend")}</h1>
                            <p>{t("auth.registerDetails")}</p>
                            <button className="auth-toggle-btn" onClick={() => setIsSignUp(true)} type="button">
                                {t("auth.signUp")}
                            </button>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    );
};

export default AuthPage;
