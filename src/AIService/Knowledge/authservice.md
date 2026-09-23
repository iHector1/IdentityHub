# AuthService

AuthService stores credentials in its SQL Server database. Passwords are hashed with BCrypt and are never returned by the API.

`POST /api/auth/register` creates credentials for an existing active user. `POST /api/auth/login` validates the email and password, verifies that the user is active, and returns a JWT with an expiration time.

The JWT uses the configured issuer, audience and signing key. Other protected services validate the same token independently.
