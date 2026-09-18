import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const NotFoundPage = () => {
    const navigate = useNavigate();
    const { t } = useTranslation();

    return (
        <div className="not-found-page">
            <div className="not-found-card">
                <p className="not-found-code">404</p>
                <h1>{t("notFound.title")}</h1>
                <p className="not-found-message">
                    {t("notFound.message")}
                </p>
                <button className="not-found-btn" onClick={() => navigate('/')}>
                    {t("notFound.backHome")}
                </button>
            </div>
        </div>
    );
};

export default NotFoundPage;
