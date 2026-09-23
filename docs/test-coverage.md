# Cobertura de pruebas

La validación actual ejecutó **99 tests**: 99 pasaron y 0 fallaron.

Comando utilizado:

```powershell
dotnet test IdentityHub.slnx --collect:"XPlat Code Coverage"
```

Resultados brutos reales del reporte Cobertura:

| Servicio | Line coverage | Branch coverage |
| --- | ---: | ---: |
| AuthService | 56.20% (154/274) | 75.00% (6/8) |
| UserService | 59.29% (201/339) | 63.63% (14/22) |
| RoleService | 79.16% (114/144) | 78.57% (11/14) |
| AIService | 85.52% (396/463) | 80.17% (93/116) |
| AuditService | 86.95% (40/46) | 100.00% (6/6) |
| **Global ponderado** | **71.48% (905/1266)** | **78.31% (130/166)** |

La cobertura global supera el objetivo mínimo de 70%. AuthService y UserService todavía tienen líneas sin cubrir principalmente en migraciones, mensajería y DTOs; no se alteró código productivo solo para inflar esos porcentajes.
