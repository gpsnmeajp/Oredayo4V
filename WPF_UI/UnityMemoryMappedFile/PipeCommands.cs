using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace UnityMemoryMappedFile
{
    public class PipeCommands
    {
        public static Type GetCommandType(string commandStr)
        {
            var commands = typeof(PipeCommands).GetNestedTypes(System.Reflection.BindingFlags.Public);
            foreach (var command in commands)
            {
                if (command.Name == commandStr) return command;
            }
            return null;
        }

        public class SendMessage
        {
            public string Message { get; set; }
        }

        public class BackgrounColor
        {
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }
        }
        public class CameraPos
        {
            public float rotate { get; set; }
            public float zoom { get; set; }
            public float height { get; set; }
        }
        public class LoadVRM
        {
            public string filepath { get; set; }
        }

    }
}
