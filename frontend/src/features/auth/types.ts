// Contract mirroring TaskManagement.Application.Users.Responses.AuthResponse.
// Field names match the backend JSON camelCase output exactly.

export interface AuthUser {
  id: string;
  email: string;
}

export interface AuthSession {
  user: AuthUser;
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
}
