# Cobertura de pruebas

La validación actual ejecutó **63 tests**, todos exitosos.

Comando utilizado:

```powershell
dotnet test IdentityHub.slnx --collect:"XPlat Code Coverage"
```

El reporte Cobertura generado por el collector muestra estos resultados brutos:

| Proyecto de tests | Line coverage | Branch coverage |
| --- | ---: | ---: |
| AuthService | 45.62% (125/274) | 50.00% (4/8) |
| UserService | 50.44% (171/339) | 59.09% (13/22) |
| RoleService | 79.16% (114/144) | 78.57% (11/14) |
| AIService | 60.42% (113/187) | 40.00% (16/40) |
| AuditService | 86.95% (40/46) | 100.00% (6/6) |
| **Total ponderado** | **56.87% (563/990)** | **55.56% (50/90)** |

La cobertura bruta es menor porque incluye infraestructura, migraciones, código generado y DTOs triviales. Estos porcentajes corresponden al reporte actual sin aplicar un filtro adicional.
