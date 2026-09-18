export interface UserProfileProps {
  id:              string;
  username:        string;
  email:           string;
  displayName?:    string;
  bio?:            string;
  targetCefrLevel?: string;
  nativeLanguage?: string;
  learningSince?:  string;
  hasAvatar:       boolean;
  createdAt:       string;
}

export interface UpdateProfilePayload {
  username?:       string;
  email?:          string;
  displayName?:    string;
  bio?:            string;
  targetCefrLevel?: string;
  nativeLanguage?: string;
  learningSince?:  string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword:     string;
}
