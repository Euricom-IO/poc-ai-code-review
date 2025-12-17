# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8.0 C# project configured with comprehensive CI/CD pipelines and security tooling for AI-assisted code review POC (Proof of Concept).

## Build and Test Commands

```bash
# Restore NuGet dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release --no-restore

# Run all tests
dotnet test --configuration Release --no-build --verbosity normal

# Run tests with coverage
dotnet test --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults
```

## CI/CD Pipeline

The project uses GitHub Actions with two workflows:

1. **.NET CI Pipeline** (`.github/workflows/dotnet-ci.yml`)
   - Triggers on PR to `main`/`develop` branches and pushes to `main`
   - Runs on: C# file changes, project file changes, solution changes
   - Steps: restore → build (Release) → test → upload test results
   - Uses .NET 8.0.x

2. **CodeQL Security Analysis** (`.github/workflows/codeql-analysis.yml`)
   - Runs security-extended and security-and-quality queries
   - Triggers: PRs, pushes to main, weekly schedule (Monday 1:30 AM UTC)
   - Automatically analyzes C# code for security vulnerabilities

## Code Review Configuration

This project uses CodeRabbit AI for automated code reviews (`.coderabbit.yaml`):

**C# Review Focus Areas:**
- C# coding conventions and .NET best practices
- SOLID principles and design patterns
- Async/await patterns
- Exception handling and IDisposable resource disposal
- Nullable reference types and null handling
- LINQ efficiency and memory allocations
- Security: SQL injection, XSS, authentication issues
- Thread safety and concurrency

**Test Code Standards:**
- Arrange-Act-Assert pattern
- Proper test framework usage (xUnit/NUnit/MSTest)
- Test naming conventions
- Mock usage and test isolation
- Coverage of edge cases

## Architecture Notes

When implementing the project structure:
- Target .NET 8.0
- Follow nullable reference type conventions
- Implement proper async/await patterns throughout
- Use IDisposable pattern for resource management
- Ensure thread-safe operations where concurrent access is possible
