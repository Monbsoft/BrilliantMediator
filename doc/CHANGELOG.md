# Changelog

Tous les changements notables du projet BrilliantMediator sont documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
et ce projet adhère à [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Support initial pour les Events (IEvent, IEventHandler)
- Extension de DependencyInjection avec `AddBrilliantMediator`
- MediatorBuilder pour une configuration fluente
- Support pour les Queries (IQuery, IQueryHandler)
- Support pour les Commands sans réponse
- Support pour les Commands avec réponse

### Changed
- Amélioration de la documentation avec exemples en français
- Optimisation de la structure de registres statiques

### Fixed
- Correction de la gestion des exceptions HandlerNotRegisteredException

---

## [1.1.0] - 2024-01-XX

### Added
- ✨ Support des événements (Events)
- 🎯 Amélioration de la détection automatique des handlers
- 📋 Extension pour DependencyInjection

### Changed
- 🔧 Refactorisation de l'architecture interne
- 📈 Optimisations de performance mineures

### Fixed
- 🐛 Correction du bug de registration des handlers
- 🐛 Amélioration de la gestion des erreurs

---

## [1.0.0] - 2025-11-06

### Added
- ✨ Implémentation de base du Mediator Pattern
- ⚡ Zero-Reflection architecture avec registres statiques génériques
- 🚀 Performance exceptionnelle (~50ns par opération)
- 🎯 Support complet du pattern CQRS
- 📦 Support des Commands avec et sans réponse
- 📚 Support des Queries
- 🔧 API simple et type-safe
- 📖 Documentation complète avec exemples

### Features
- ✅ Commands sans réponse (ICommand)
- ✅ Commands avec réponse (ICommand<TResponse>)
- ✅ Queries (IQuery<TResponse>)
- ✅ Enregistrement manuel des handlers
- ✅ Gestion des exceptions avec HandlerNotRegisteredException
- ✅ API asynchrone complète
- ✅ Aucune dépendance externe

---

## Guide d'utilisation du Changelog

### Pour les contributeurs

Quand vous créez une Pull Request, mettez à jour le section `[Unreleased]`:

1. **Added** - Pour les nouvelles fonctionnalités
2. **Changed** - Pour les changements sur les fonctionnalités existantes
3. **Deprecated** - Pour les fonctionnalités qui seront supprimées
4. **Removed** - Pour les fonctionnalités supprimées
5. **Fixed** - Pour les bug fixes
6. **Security** - Pour les corrections de sécurité

### Exemple

```markdown
### Added
- ✨ Nouvelle fonctionnalité X
- ⚡ Support pour Y

### Fixed
- 🐛 Bug Z corrigé
```

### Convention de tags

- ✨ - Feature/Amélioration
- ⚡ - Performance
- 🚀 - Launch/Déploiement
- 🎯 - Objectif/Focus
- 📦 - Packaging/Distribution
- 🔧 - Configuration/Maintenance
- 📚 - Documentation
- 🐛 - Bug fix
- ✅ - Complete/Validation
- 📖 - Guide
- ❌ - Removal/Deprecated

---

## Versioning

Ce projet utilise [Semantic Versioning](https://semver.org/):

- **MAJOR** - Changements incompatibles avec les versions précédentes
- **MINOR** - Nouvelles fonctionnalités compatibles
- **PATCH** - Corrections de bugs compatibles

Exemple: v1.2.3
- 1 = MAJOR
- 2 = MINOR
- 3 = PATCH
