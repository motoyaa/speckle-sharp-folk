﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.Core.Credentials
{
  public class StreamWrapper
  {
    private string originalInput;

    public string AccountId { get; set; }
    public string ServerUrl { get; set; }
    public string StreamId { get; set; }
    public string CommitId { get; set; }
    public string BranchName { get; set; }
    public string ObjectId { get; set; }

    /// <summary>
    /// Determines if the current stream wrapper contains a valid stream.
    /// </summary>
    public bool IsValid => Type != StreamWrapperType.Undefined;

    public StreamWrapperType Type
    {
      // Quick solution to determine whether a wrapper points to a branch, commit or stream.
      get
      {
        if (!string.IsNullOrEmpty(ObjectId))
        {
          return StreamWrapperType.Object;
        }

        if (!string.IsNullOrEmpty(BranchName))
        {
          return StreamWrapperType.Branch;
        }

        if (!string.IsNullOrEmpty(CommitId))
        {
          return StreamWrapperType.Commit;
        }

        if (!string.IsNullOrEmpty(StreamId))
        {
          return StreamWrapperType.Stream;
        }

        // If we reach here, it means that the stream is invalid for some reason.
        return StreamWrapperType.Undefined;
      }
    }

    public StreamWrapper()
    {
    }

    /// <summary>
    /// Creates a StreamWrapper from a stream url or a stream id
    /// </summary>
    /// <param name="streamUrlOrId">Stream Url eg: http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179  or stream ID eg: 8fecc9aa6d</param>
    /// <exception cref="Exception"></exception>
    public StreamWrapper(string streamUrlOrId)
    {
      originalInput = streamUrlOrId;

      Uri uri;
      try
      {
        if (!Uri.TryCreate(streamUrlOrId, UriKind.Absolute, out uri))
        {
          StreamWrapperFromId(streamUrlOrId);
        }
        else
        {
          StreamWrapperFromUrl(streamUrlOrId);
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
    }

    /// <summary>
    /// Creates a StreamWrapper by streamId, accountId and serverUrl
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="accountId"></param>
    /// <param name="serverUrl"></param>
    public StreamWrapper(string streamId, string accountId, string serverUrl)
    {
      AccountId = accountId;
      ServerUrl = serverUrl;
      StreamId = streamId;

      originalInput = $"{ServerUrl}/streams/{StreamId}{(AccountId != null ? "?u=" + AccountId : "")}";
    }

    private void StreamWrapperFromId(string streamId)
    {
      Account account = AccountManager.GetDefaultAccount();

      if (account == null)
      {
        throw new Exception(
          $"You do not have any account. Please create one or add it to the Speckle Manager.");
      }

      ServerUrl = account.serverInfo.url;
      AccountId = account.id;
      StreamId = streamId;
    }

    private void StreamWrapperFromUrl(string streamUrl)
    {
      Uri uri = new Uri(streamUrl);
      ServerUrl = uri.GetLeftPart(UriPartial.Authority);

      if (uri.Segments.Length < 3)
      {
        throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
      }

      switch (uri.Segments.Length)
      {
        case 3: // ie http://speckle.server/streams/8fecc9aa6d
          if (uri.Segments[1].ToLowerInvariant() == "streams/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
          }
          else
          {
            throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
          }

          break;
        case 5: // ie http://speckle.server/streams/8fecc9aa6d/commits/76a23d7179
          if (uri.Segments[3].ToLowerInvariant() == "commits/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            CommitId = uri.Segments[4].Replace("/", "");
          }
          else if (uri.Segments[3].ToLowerInvariant() == "branches/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            BranchName = Uri.UnescapeDataString(uri.Segments[4].Replace("/", ""));
          }
          else if (uri.Segments[3].ToLowerInvariant() == "objects/")
          {
            StreamId = uri.Segments[2].Replace("/", "");
            ObjectId = uri.Segments[4].Replace("/", "");
          }
          else
          {
            throw new Exception($"Cannot parse {uri} into a stream wrapper class.");
          }

          break;
      }
    }

    private Account _Account;

    /// <summary>
    /// Gets a valid account for this stream wrapper. 
    /// <para>Note: this method ensures that the stream exists and/or that the user has an account which has access to that stream. If used in a sync manner, make sure it's not blocking.</para>
    /// </summary>
    /// <exception cref="Exception">Throws exception if account fetching failed. This could be due to non-existent account or stream.</exception>
    /// <returns>The valid account object for this stream.</returns>
    public async Task<Account> GetAccount()
    {
      Exception err = null;
      
      if (_Account != null)
      {
        return _Account;
      }

      // Step 1: check if direct account id (?u=)
      if (originalInput.Contains("?u="))
      {
        var userId = originalInput.Split(new string[] {"?u="}, StringSplitOptions.None)[1];
        var acc = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == userId);
        if (acc != null)
        {
          try
          {
            await ValidateWithAccount(acc);
            _Account = acc;
            return acc;
          }
          catch(Exception e)
          {
            // If user specified account and fails, we should stop trying.
            throw e;
          }
        }
      }

      // Step 2: check the default
      var defAcc = AccountManager.GetDefaultAccount();
      try
      {
        await ValidateWithAccount(defAcc);
        _Account = defAcc;
        return defAcc;
      }
      catch(Exception e)
      {
        err = e;
      }

      // Step 3: all the rest
      var accs = AccountManager.GetAccounts(ServerUrl);
      if (accs.Count() == 0)
      {
        throw new Exception($"You don't have any accounts for ${ServerUrl}.");
      }

      foreach (var acc in accs)
      {
        try
        {
          await ValidateWithAccount(acc);
          _Account = acc;
          return acc;
        }
        catch(Exception e)
        {
          err = e;
        }
      }

      throw err;
    }
    
    public bool Equals(StreamWrapper wrapper)
    {
      if (wrapper == null) return false;
      if (Type != wrapper.Type) return false;
      return Type == wrapper.Type
             && ServerUrl == wrapper.ServerUrl
             && AccountId == wrapper.AccountId
             && StreamId == wrapper.StreamId
             && (Type == StreamWrapperType.Branch && BranchName == wrapper.BranchName)
             || (Type == StreamWrapperType.Object && ObjectId == wrapper.ObjectId)
             || (Type == StreamWrapperType.Commit && CommitId == wrapper.CommitId);
    }
    
    private async Task ValidateWithAccount(Account acc)
    {
      var client = new Client(acc);
      // First check if the stream exists
      try
      {
        await client.StreamGet(StreamId);
      }
      catch
      {
        throw new Exception(
          $"You don't have access to stream {StreamId} on server {ServerUrl}, or the stream does not exist.");
      }
      
      // Check if the branch exists
      if (Type == StreamWrapperType.Branch && await client.BranchGet(StreamId, BranchName, 1) == null)
        throw new Exception(
            $"The branch with name '{BranchName}' doesn't exist in stream {StreamId} on server {ServerUrl}");
    }

    public override string ToString()
    {
      var url = $"{ServerUrl}/streams/{StreamId}";
      switch (Type)
      {
        case StreamWrapperType.Commit:
          url += $"/commits/{CommitId}";
          break;
        case StreamWrapperType.Branch:
          url += $"/branches/{BranchName}";
          break;
        case StreamWrapperType.Object:
          url += $"/objects/{ObjectId}";
          break;
      }
      var acc =  $"{(AccountId != null ? "?u=" + AccountId : "")}";
      return url + acc;
    }
  }

  public enum StreamWrapperType
  {
    Undefined,
    Stream,
    Commit,
    Branch,
    Object
  }
}
