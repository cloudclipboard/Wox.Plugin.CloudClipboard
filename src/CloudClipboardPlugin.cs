using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Wox.Plugin.CloudClipboard
{
    public class CloudClipboardPlugin : IPlugin
    {
        private PluginInitContext _context;
        private readonly List<string> _dataList;
        private readonly Uri _url;
        private bool _isMonitoring;
        private static readonly HttpClient _client = new HttpClient();

        public int MaxDataCount { get; private set; }
        

        public CloudClipboardPlugin()
        {

        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            ClipboardMonitor.OnClipboardChange += CloudClipboardMonitor_OnClipboardChange;
            ClipboardMonitor.Start();
            _isMonitoring = true;
        }

        private void CloudClipboardMonitor_OnClipboardChange(ClipboardFormat format, object data)
        {
            if (format == ClipboardFormat.Html ||
                format == ClipboardFormat.SymbolicLink ||
                format == ClipboardFormat.Text ||
                format == ClipboardFormat.UnicodeText)
            {
                if (data != null && !string.IsNullOrEmpty(data.ToString().Trim()))
                {
                    if (_dataList.Contains(data.ToString()))
                    {
                        _dataList.Remove(data.ToString());
                    }
                    _dataList.Add(data.ToString());
                    var keypair = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("data", data.ToString())
                    };
                    var content = new FormUrlEncodedContent(keypair);
                    var result = _client.PostAsync(_url, content).Result;

                    if (_dataList.Count > MaxDataCount)
                    {
                        _dataList.Remove(_dataList.Last());
                    }
                }
            }
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            List<string> displayData;

            // Keyword stop - stops the monitoring
            if(query.ActionParameters.Count > 0 
                && query.GetAllRemainingParameter().ToLower() == "stop"
                && _isMonitoring)
            {
                ClipboardMonitor.Stop();

                results.Add(new Result
                {
                    Title = "Cloud clipmonitor stoped",
                    IcoPath = "Images\\cloudclipboard.png"
                });
                return results;
            }

            // Keyword start - starts the monitoring
            if (query.ActionParameters.Count > 0
                && query.GetAllRemainingParameter().ToLower() == "start"
                && !_isMonitoring)
            {
                ClipboardMonitor.Start();
                results.Add(new Result
                {
                    Title = "Cloud clipmonitor started",
                    IcoPath = "Images\\cloudclipboard.png"
                });
                return results;
            }

            // Search
            if (query.ActionParameters.Count == 0)
            {
                displayData = _dataList;
            }
            else
            {
                displayData = _dataList.Where(i => i.ToLower().Contains(query.GetAllRemainingParameter().ToLower()))
                        .ToList();
            }

            results.AddRange(displayData.Select(o => new Result
            {
                Title = o.Trim(),
                IcoPath = "Images\\cloudclipboard.png",
                Action = c =>
                {
                    try
                    {
                        Clipboard.SetText(o);
                        return true;
                    }
                    catch (Exception e)
                    {
                        _context.API.ShowMsg("Error", e.Message, null);
                        return false;
                    }
                }
            }).Reverse());
            return results;
        }
    }
}
