using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using tobid.util.http.ws.cmd;

namespace tobid.util.http.ws {

    public abstract class JsonCreationConverter<T> : Newtonsoft.Json.JsonConverter {

        protected abstract T Create(Type objectType, JObject jObject);

        public override bool CanConvert(Type objectType) {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {

            JObject jObject = JObject.Load(reader);
            T target = Create(objectType, jObject);
            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {

            throw new NotImplementedException();
        }
    }

    public class CommandConvert : JsonCreationConverter<Command> {

        protected override Command Create(Type objectType, JObject jObject) {

            JValue category = (JValue)this.GetType("category", jObject);
            String value = category.ToString();
            if ("MESSAGE".Equals(value))
                return new Message();
            else if ("HEARTBEAT".Equals(value))
                return new HeartBeat();
            else if ("SETTIMER".Equals(value))
                return new SetTimerCmd();
            else if ("RELOAD".Equals(value))
                return new ReloadCmd();
            else if ("TRIGGERF11".Equals(value))
                return new TriggerF11Cmd();
            else if ("UPDATEPOLICY".Equals(value))
                return new UpdatePolicyCmd();
            else if ("RETRY".Equals(value))
                return new Retry();
            else if ("REPLY".Equals(value))
                return new Reply();
            else if ("SETTRIGGER".Equals(value))
                return new SetTriggerCmd();
            else if ("TIMESYNC".Equals(value))
                return new TimeSyncCmd();

            return new Other();
        }

        private Object GetType(String prop, JObject jObject) {
            return jObject[prop];
        }
    }
}
