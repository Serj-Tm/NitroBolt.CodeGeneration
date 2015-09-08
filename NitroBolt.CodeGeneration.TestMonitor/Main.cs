using NitroBolt.Functional;
using NitroBolt.Wui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NitroBolt.CodeGeneration
{
    public class Main
    {
        private static MonitorState Execute(JsonData[] jsons, MonitorState state)
        {

            foreach (var json in jsons.OrEmpty())
            {
                switch (json.JPath("data", "command")?.ToString())
                {

                }
            }
            return state;
        }

        public static HtmlResult<HElement> HView(object _state, JsonData[] jsons, HContext context)
        {
            var state = _state.As<MonitorState>() ?? new MonitorState();

            state = Execute(jsons, state);

            var page = h.Html(
              h.Head
              (
              ),
              h.Body
              (
                  h.Div(DateTime.UtcNow)
              )
            );
            return new HtmlResult<HElement>
            {
                Html = page,
                State = state,
            };

        }
        static readonly HBuilder h = null;


    }

    public partial class MonitorState
    {

    }
}