using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;
#if NET8_0_OR_GREATER
using NAudio.Wave;
#endif
namespace Musikbox {
 public sealed class SpeechCapture:IDisposable {readonly MemoryStream clip=new MemoryStream();byte[] pre=new byte[0];int voiced,silence;public byte[] Feed(byte[] data,int count){bool speech=SpeechPhrases.Peak16(data,count)>.015;if(clip.Length==0&&!speech){pre=data.Take(count).ToArray();return null;}if(clip.Length==0)clip.Write(pre,0,pre.Length);clip.Write(data,0,count);if(speech){voiced+=count;silence=0;}else silence+=count;return silence>=16000||clip.Length>=256000?Finish():null;}public byte[] Finish(){byte[] result=voiced>=6400?clip.ToArray():null;clip.SetLength(0);voiced=silence=0;pre=new byte[0];return result;}public void Dispose(){clip.Dispose();}}
}
