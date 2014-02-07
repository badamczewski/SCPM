#region Licence
/*
Copyright (c) 2011-2014 Contributors as noted in the AUTHORS file

This file is part of SCPM.

SCPM is free software; you can redistribute it and/or modify it under
the terms of the GNU Lesser General Public License as published by
the Free Software Foundation; either version 3 of the License, or
(at your option) any later version.

SCPM is distributed WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

You should have received a copy of the GNU Lesser General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace SCPM.Common
{
    public class Unsafe
    {
        /// <summary>
        /// Gets the physcial cores and should this fail returns the Environment.ProcessorCount
        /// that can also include logical cores.
        /// </summary>
        /// <returns>core count.</returns>
        public static uint GetPhysicalCores()
        {
            try
            {
                ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");
                ObjectQuery query = new ObjectQuery("SELECT NumberOfCores FROM Win32_Processor");

                ManagementObjectSearcher searcher =
                            new ManagementObjectSearcher(scope, query);

                ManagementObjectCollection queryCollection = searcher.Get();
                var enumerator = queryCollection.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    ManagementObject obj = (ManagementObject)enumerator.Current;
                    return (uint)obj["NumberOfCores"];
                }
                else
                    return (uint)Environment.ProcessorCount;
            }
            // We are not very interested in throwing the exception, we might log it
            // but this carries little information as if WMI fails there is very little
            // that the we or the user can do to provide the correct information.
            catch
            {
                return (uint)Environment.ProcessorCount;
            }
        }
    }
}
