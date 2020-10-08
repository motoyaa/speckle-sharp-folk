﻿using System;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen
  {
    private readonly IEventAggregator _events;
    private readonly ViewManager _viewManager;
    private readonly IDialogFactory _dialogFactory;
    private readonly ConnectorBindings _bindings;

    public StreamViewModel(
      IEventAggregator events,
      ViewManager viewManager,
      IDialogFactory dialogFactory,
      ConnectorBindings bindings)
    {
      _events = events;
      _viewManager = viewManager;
      _dialogFactory = dialogFactory;
      _bindings = bindings;
    }

    private StreamState _streamState;

    public StreamState StreamState
    {
      get => _streamState;
      set
      {
        SetAndNotify(ref _streamState, value);
        Stream = StreamState.stream;
        Branch = StreamState.stream.branches.items[ 0 ];
      }
    }

    private Stream _stream;

    public Stream Stream
    {
      get => _stream;
      set => SetAndNotify(ref _stream, value);
    }

    private Branch _branch;

    public Branch Branch
    {
      get => _branch;
      set => SetAndNotify(ref _branch, value);
    }


    public async void ConvertAndSendObjects()
    {
      if ( !StreamState.placeholders.Any() )
      {
        _bindings.RaiseNotification("Nothing to send to Speckle.");
        return;
      }

      try
      {
        StreamState = await _bindings.SendStream(StreamState);
      }
      catch ( Exception e )
      {
        _bindings.RaiseNotification($"Error: {e.Message}");
        return;
      }
      NotifyOfPropertyChange(nameof(StreamState));
    }

    public async void ShowStreamUpdateDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateStreamUpdateDialog();
      viewmodel.StreamState = StreamState;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    public async void ShowShareDialog(StreamState state)
    {
      var viewmodel = _dialogFactory.CreateShareStreamDialogViewModel();
      viewmodel.StreamState = StreamState;
      var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewmodel);

      var result = await DialogHost.Show(view, "StreamDialogHost");
    }

    // TODO figure out how to call this from parent instead of
    // rewriting the method here
    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent() {Notification = "Stream ID copied to clipboard!"});
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }
  }
}
