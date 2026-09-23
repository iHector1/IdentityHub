# UserService

UserService manages users in SQL Server. A user has a first name, last name, normalized email, active status, creation timestamp and optional update timestamp.

The protected API exposes user listing, creation and update operations. User status is represented by `IsActive`; deactivation is a status change and does not physically delete the user.

UserService also exposes an internal user lookup used by other services. That internal endpoint requires the configured `X-Internal-Api-Key`.
