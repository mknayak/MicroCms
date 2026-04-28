using HotChocolate;
using MediatR;
using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using MicroCMS.Application.Features.Entries.Commands.DeleteEntry;
using MicroCMS.Application.Features.Entries.Commands.PublishEntry;
using MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;
using MicroCMS.Application.Features.Entries.Commands.UpdateEntry;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.GraphQL.Types;

namespace MicroCMS.GraphQL.Mutations;

/// <summary>Root GraphQL mutation type — mirrors the REST write surface.</summary>
[GraphQLName("Mutation")]
public sealed class RootMutation
{
 // ── Entry mutations ────────────────────────────────────────────────────

    /// <summary>Creates a new content entry in Draft status.</summary>
    public async Task<EntryPayload> CreateEntryAsync(
        CreateEntryInput input,
  [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateEntryCommand(
            input.SiteId,
      input.ContentTypeId,
            input.Slug,
        input.Locale,
            input.FieldsJson ?? "{}");

   var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
       ? EntryPayload.Ok(result.Value)
         : EntryPayload.Fail(result.Error.Message);
    }

    /// <summary>Updates an existing entry's field data.</summary>
    public async Task<EntryPayload> UpdateEntryAsync(
 UpdateEntryInput input,
        [Service] IMediator mediator,
CancellationToken cancellationToken)
    {
        var command = new UpdateEntryCommand(
   input.EntryId,
 input.FieldsJson,
        input.NewSlug,
   input.ChangeNote);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
  ? EntryPayload.Ok(result.Value)
            : EntryPayload.Fail(result.Error.Message);
    }

    /// <summary>Publishes an Approved or Scheduled entry immediately.</summary>
    public async Task<EntryPayload> PublishEntryAsync(
        Guid entryId,
  [Service] IMediator mediator,
  CancellationToken cancellationToken)
    {
   var result = await mediator.Send(new PublishEntryCommand(entryId), cancellationToken);
        return result.IsSuccess
      ? EntryPayload.Ok(result.Value)
      : EntryPayload.Fail(result.Error.Message);
    }

    /// <summary>Moves a Published entry back to Unpublished status.</summary>
public async Task<EntryPayload> UnpublishEntryAsync(
        Guid entryId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
   var result = await mediator.Send(new UnpublishEntryCommand(entryId), cancellationToken);
        return result.IsSuccess
            ? EntryPayload.Ok(result.Value)
            : EntryPayload.Fail(result.Error.Message);
    }

    /// <summary>Permanently deletes an entry.</summary>
 public async Task<DeletePayload> DeleteEntryAsync(
        Guid entryId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteEntryCommand(entryId), cancellationToken);
        return result.IsSuccess
  ? DeletePayload.Ok(entryId)
            : DeletePayload.Fail(result.Error.Message);
    }

    // ── ContentType mutations ──────────────────────────────────────────────

    /// <summary>Creates a new content type.</summary>
 public async Task<ContentTypePayload> CreateContentTypeAsync(
     CreateContentTypeInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateContentTypeCommand(
     input.SiteId,
            input.Handle,
      input.DisplayName,
      input.Description);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? ContentTypePayload.Ok(result.Value)
            : ContentTypePayload.Fail(result.Error.Message);
    }

    /// <summary>Adds a field to an existing content type.</summary>
    public async Task<ContentTypePayload> AddFieldAsync(
        AddFieldInput input,
        [Service] IMediator mediator,
      CancellationToken cancellationToken)
    {
        var command = new AddFieldCommand(
     input.ContentTypeId,
       input.Handle,
          input.Label,
       input.FieldType,
      input.IsRequired,
         input.IsLocalized,
            input.IsUnique,
 false, // IsIndexed — not exposed via GraphQL input (defaults to false)
   false, // IsList — not exposed via GraphQL input (defaults to false)
      input.Description);

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
   ? ContentTypePayload.Ok(result.Value)
            : ContentTypePayload.Fail(result.Error.Message);
    }

    /// <summary>Removes a field from a content type.</summary>
    public async Task<ContentTypePayload> RemoveFieldAsync(
        Guid contentTypeId,
        Guid fieldId,
        [Service] IMediator mediator,
      CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveFieldCommand(contentTypeId, fieldId), cancellationToken);
     return result.IsSuccess
     ? ContentTypePayload.Ok(result.Value)
   : ContentTypePayload.Fail(result.Error.Message);
    }

    /// <summary>Publishes a content type, making it available for content creation.</summary>
    public async Task<ContentTypePayload> PublishContentTypeAsync(
        Guid contentTypeId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
 new PublishContentTypeCommand(contentTypeId), cancellationToken);
   return result.IsSuccess
            ? ContentTypePayload.Ok(result.Value)
            : ContentTypePayload.Fail(result.Error.Message);
    }
}
