# RoleService

RoleService stores roles and user-role assignments in SQL Server. A role has a name, optional description, active status and timestamps.

When assigning a role, RoleService checks UserService through its internal user endpoint. The assignment is rejected when the user or role does not exist, when the user is inactive, or when the role is inactive.

Role creation, role assignment and role removal publish integration events through RabbitMQ.
