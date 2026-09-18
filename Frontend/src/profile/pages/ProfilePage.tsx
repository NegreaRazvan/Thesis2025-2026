import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import toast from "react-hot-toast";
import { useAuthContext } from "../../auth/context/AuthContext";
import { useLanguage } from "../../App";
import useUserApi from "../useUserApi";
import useProgressApi from "../../progress/useProgressApi";
import AvatarEditor from "../components/AvatarEditor";
import ProfileSelect from "../components/ProfileSelect";
import type { UserProfileProps } from "../props";
import "../profile.css";

const CEFR_OPTIONS = ["A1", "A2", "B1", "B2", "C1", "C2"].map(l => ({ value: l, label: l }));

const ProfilePage = () => {
    const { user, updateUser, logout } = useAuthContext();
    const { getProfile, updateProfile, changePassword, uploadAvatar, deleteAvatar } = useUserApi();
    const { getGamification, getTimeline } = useProgressApi();
    const { lang, setLanguage } = useLanguage();
    const { t } = useTranslation();
    const navigate = useNavigate();

    const [profile, setProfile] = useState<UserProfileProps | null>(null);
    const [loading, setLoading] = useState(true);

    const [email,       setEmail]       = useState("");
    const [displayName, setDisplayName] = useState("");
    const [bio,         setBio]         = useState("");
    const [savingProfile, setSavingProfile] = useState(false);

    const [targetLevel,    setTargetLevel]    = useState("");
    const [nativeLang,     setNativeLang]     = useState("");
    const [learningSince,  setLearningSince]  = useState("");
    const [savingLearning, setSavingLearning] = useState(false);

    const [currentPw,  setCurrentPw]  = useState("");
    const [newPw,      setNewPw]      = useState("");
    const [confirmPw,  setConfirmPw]  = useState("");
    const [savingPw,   setSavingPw]   = useState(false);

    const [avatarUploading, setAvatarUploading] = useState(false);

    const [streak, setStreak]     = useState(0);
    const [submissions, setSubmissions] = useState(0);
    const [cefrLevel, setCefrLevel] = useState("—");

    useEffect(() => {
        Promise.all([getProfile(), getGamification(), getTimeline()])
            .then(([p, g, timeline]) => {
                setProfile(p);
                setEmail(p.email);
                setDisplayName(p.displayName ?? "");
                setBio(p.bio ?? "");
                setTargetLevel(p.targetCefrLevel ?? "");
                setNativeLang(p.nativeLanguage ?? "");
                setLearningSince(p.learningSince ?? "");
                setStreak(g.currentStreak);
                setSubmissions(g.totalSubmissions);
                const last = timeline[timeline.length - 1];
                if (last?.predictedCefr) setCefrLevel(last.predictedCefr);
            })
            .catch(() => toast.error(t("profile.loadProfileError")))
            .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleSaveProfile = async (e: React.FormEvent) => {
        e.preventDefault();
        setSavingProfile(true);
        try {
            const updated = await updateProfile({
                email:       email !== profile?.email ? email : undefined,
                displayName: displayName || undefined,
                bio:         bio || undefined,
            });
            setProfile(updated);
            updateUser({
                username:    updated.username,
                email:       updated.email,
                displayName: updated.displayName,
            });
            toast.success(t("profile.profileSaved"));
        } catch {
            toast.error(t("profile.profileSaveError"));
        } finally {
            setSavingProfile(false);
        }
    };

    const handleSaveLearning = async (e: React.FormEvent) => {
        e.preventDefault();
        setSavingLearning(true);
        try {
            const updated = await updateProfile({
                targetCefrLevel: targetLevel || undefined,
                nativeLanguage:  nativeLang || undefined,
                learningSince:   learningSince || undefined,
            });
            setProfile(updated);
            toast.success(t("profile.learningProfileSaved"));
        } catch {
            toast.error(t("profile.learningProfileError"));
        } finally {
            setSavingLearning(false);
        }
    };

    const handleChangePassword = async (e: React.FormEvent) => {
        e.preventDefault();
        if (newPw !== confirmPw) { toast.error(t("profile.passwordMismatch")); return; }
        setSavingPw(true);
        try {
            await changePassword({ currentPassword: currentPw, newPassword: newPw });
            setCurrentPw(""); setNewPw(""); setConfirmPw("");
            toast.success(t("profile.passwordChanged"));
        } catch {
            toast.error(t("profile.passwordError"));
        } finally {
            setSavingPw(false);
        }
    };

    const handleAvatarUpload = async (file: File) => {
        setAvatarUploading(true);
        try {
            await uploadAvatar(file);
            const ver = (user?.avatarVersion ?? 0) + 1;
            updateUser({ avatarVersion: ver });
            setProfile(p => p ? { ...p, hasAvatar: true } : p);
            toast.success(t("profile.avatarUpdated"));
        } catch {
            toast.error(t("profile.avatarError"));
        } finally {
            setAvatarUploading(false);
        }
    };

    const handleAvatarRemove = async () => {
        try {
            await deleteAvatar();
            updateUser({ avatarVersion: 0 });
            setProfile(p => p ? { ...p, hasAvatar: false } : p);
            toast.success(t("profile.avatarRemoved"));
        } catch {
            toast.error(t("profile.avatarRemoveError"));
        }
    };

    const handleSignOut = () => {
        logout();
        navigate("/login");
    };

    const initials = (profile?.displayName ?? profile?.username ?? "??").slice(0, 2).toUpperCase();
    const displayLabel = profile?.displayName || profile?.username || "—";

    const memberYear = profile?.createdAt
        ? new Date(profile.createdAt).getFullYear()
        : "—";

    if (loading) {
        return (
            <div className="profile-root">
                <div className="masthead">
                    <div className="mh-number">08</div>
                    <div><div className="mh-title">{t("profile.title")}</div></div>
                </div>
            </div>
        );
    }

    return (
        <div className="profile-root">
            <div className="masthead">
                <div className="mh-number">08</div>
                <div>
                    <div className="mh-title">{t("profile.title")}</div>
                    <div className="mh-sub">{t("profile.sub")}</div>
                </div>
            </div>

            <div className="profile-header">
                <AvatarEditor
                    initials={initials}
                    hasAvatar={profile?.hasAvatar ?? false}
                    avatarVersion={user?.avatarVersion}
                    uploading={avatarUploading}
                    onUpload={handleAvatarUpload}
                    onRemove={handleAvatarRemove}
                />
                <div className="profile-identity">
                    <div className="profile-identity-name">{displayLabel}</div>
                    <div className="profile-identity-meta">
                        <span>@{profile?.username}</span>
                        <span>{profile?.email}</span>
                        <span>{t("profile.memberSince", { year: memberYear })}</span>
                    </div>
                    {cefrLevel !== "—" && (
                        <div className="profile-cefr-badge">
                            <span className="profile-cefr-badge-level">{cefrLevel}</span>
                            {t("profile.currentLevel")}
                        </div>
                    )}
                </div>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">01</span>
                    <span className="profile-section-title">{t("profile.sectionIdentity")}</span>
                </div>
                <form className="profile-form" onSubmit={handleSaveProfile}>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.username")}</label>
                        <div className="profile-field-input-wrap" style={{ opacity: 0.55, userSelect: "none" }}>
                            <span style={{ fontFamily: "var(--font-ui)", fontSize: 14, color: "var(--text)" }}>
                                @{profile?.username}
                            </span>
                            <span style={{ fontFamily: "var(--font-mono)", fontSize: 9, color: "var(--text-mute)", marginLeft: 10, textTransform: "uppercase", letterSpacing: "0.1em" }}>
                                {t("profile.locked")}
                            </span>
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.emailLabel")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="email" value={email}
                                onChange={e => setEmail(e.target.value)}
                                placeholder={t("profile.emailPlaceholder")}
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.displayName")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="text" value={displayName} maxLength={80}
                                onChange={e => setDisplayName(e.target.value)}
                                placeholder={t("profile.displayNamePlaceholder")}
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.bio")}</label>
                        <div className="profile-field-input-wrap">
                            <textarea
                                value={bio} maxLength={500}
                                onChange={e => setBio(e.target.value)}
                                placeholder={t("profile.bioPlaceholder")}
                                rows={3}
                            />
                        </div>
                    </div>
                    <div className="profile-form-actions">
                        <button className="profile-btn profile-btn--primary" type="submit" disabled={savingProfile}>
                            {savingProfile ? t("profile.saving") : t("profile.saveIdentity")}
                        </button>
                    </div>
                </form>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">02</span>
                    <span className="profile-section-title">{t("profile.sectionLearning")}</span>
                </div>
                <form className="profile-form" onSubmit={handleSaveLearning}>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.targetLevel")}</label>
                        <div className="profile-field-input-wrap">
                            <ProfileSelect
                                value={targetLevel}
                                onChange={setTargetLevel}
                                options={CEFR_OPTIONS}
                                placeholder={t("profile.selectTarget")}
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.nativeLanguage")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="text" value={nativeLang} maxLength={50}
                                onChange={e => setNativeLang(e.target.value)}
                                placeholder={t("profile.nativeLangPlaceholder")}
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.learningSince")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="date" value={learningSince}
                                onChange={e => setLearningSince(e.target.value)}
                            />
                        </div>
                    </div>
                    <div className="profile-form-actions">
                        <button className="profile-btn profile-btn--primary" type="submit" disabled={savingLearning}>
                            {savingLearning ? t("profile.savingLearning") : t("profile.saveLearning")}
                        </button>
                    </div>
                </form>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">03</span>
                    <span className="profile-section-title">{t("profile.sectionPreferences")}</span>
                </div>
                <div className="profile-form">
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.language")}</label>
                        <div className="profile-field-input-wrap">
                            <div className="sb-theme-toggle" style={{ width: "fit-content" }}>
                                <button
                                    className={`sb-theme-btn ${lang === "en" ? "sb-theme-btn--active" : ""}`}
                                    type="button"
                                    onClick={() => setLanguage("en")}
                                    style={{ gap: "0.4rem", display: "inline-flex", alignItems: "center" }}
                                >
                                    <span style={{ fontWeight: 700, letterSpacing: "0.04em" }}>EN</span>
                                    <span>English</span>
                                </button>
                                <button
                                    className={`sb-theme-btn ${lang === "de" ? "sb-theme-btn--active" : ""}`}
                                    type="button"
                                    onClick={() => setLanguage("de")}
                                    style={{ gap: "0.4rem", display: "inline-flex", alignItems: "center" }}
                                >
                                    <span style={{ fontWeight: 700, letterSpacing: "0.04em" }}>DE</span>
                                    <span>Deutsch</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">04</span>
                    <span className="profile-section-title">{t("profile.sectionActivity")}</span>
                </div>
                <div className="profile-stats-grid">
                    <div className="profile-stat">
                        <div className="profile-stat-value" style={{ color: "var(--cefr-b1)" }}>{cefrLevel}</div>
                        <div className="profile-stat-label">{t("profile.currentLevel")}</div>
                    </div>
                    <div className="profile-stat">
                        <div className="profile-stat-value">{submissions}</div>
                        <div className="profile-stat-label">{t("profile.submissions")}</div>
                    </div>
                    <div className="profile-stat">
                        <div className="profile-stat-value" style={{ color: "var(--red)" }}>{streak}</div>
                        <div className="profile-stat-label">{t("profile.streak")}</div>
                    </div>
                    <div className="profile-stat">
                        <div className="profile-stat-value">{memberYear}</div>
                        <div className="profile-stat-label">{t("profile.memberSinceLabel")}</div>
                    </div>
                </div>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">05</span>
                    <span className="profile-section-title">{t("profile.sectionSecurity")}</span>
                </div>
                <form className="profile-form" onSubmit={handleChangePassword}>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.currentPasswordLabel")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="password" value={currentPw} required
                                onChange={e => setCurrentPw(e.target.value)}
                                placeholder={t("profile.pwPlaceholder")}
                                autoComplete="current-password"
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.newPasswordLabel")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="password" value={newPw} required minLength={8}
                                onChange={e => setNewPw(e.target.value)}
                                placeholder={t("profile.newPwPlaceholder")}
                                autoComplete="new-password"
                            />
                        </div>
                    </div>
                    <div className="profile-field">
                        <label className="profile-field-label">{t("profile.confirmPasswordLabel")}</label>
                        <div className="profile-field-input-wrap">
                            <input
                                type="password" value={confirmPw} required
                                onChange={e => setConfirmPw(e.target.value)}
                                placeholder={t("profile.repeatPwPlaceholder")}
                                autoComplete="new-password"
                            />
                        </div>
                    </div>
                    <div className="profile-form-actions">
                        <button className="profile-btn profile-btn--primary" type="submit" disabled={savingPw}>
                            {savingPw ? t("profile.changing") : t("profile.changePassword")}
                        </button>
                    </div>
                </form>
            </div>

            <div className="profile-section">
                <div className="profile-section-head">
                    <span className="profile-section-num">06</span>
                    <span className="profile-section-title">{t("profile.sectionDanger")}</span>
                </div>
                <div className="profile-danger-card">
                    <div>
                        <div className="profile-danger-text">{t("profile.signOutText")}</div>
                        <div className="profile-danger-sub">{t("profile.signOutSub")}</div>
                    </div>
                    <button className="profile-btn profile-btn--danger" type="button" onClick={handleSignOut}>
                        {t("profile.signOut")}
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ProfilePage;
