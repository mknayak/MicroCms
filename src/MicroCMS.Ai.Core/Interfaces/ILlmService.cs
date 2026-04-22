// LlmRequest, LlmResponse, and ILlmService are all defined in
// MicroCMS.Application.Common.Interfaces. Ai.Core implements that contract;
// it must never redefine these types (ADR-011: provider swap via config only).
//
// Concrete implementations (e.g. LlmServiceRouter) live in MicroCMS.Ai.Core
// and are registered against MicroCMS.Application.Common.Interfaces.ILlmService
// so application-layer handlers never take a compile-time dependency on Ai.Core.
//
// Provider-specific provider interfaces belong in MicroCMS.Ai.Abstractions.
