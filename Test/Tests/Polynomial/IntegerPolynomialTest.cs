#region Directives
using System;
using NTRU.Encrypt;
using NTRU.Polynomial;
using VTDev.Libraries.CEXEngine.Crypto.Numeric;
using VTDev.Libraries.CEXEngine.Utility;
using Test.Tests.Encode;
#endregion

namespace Test.Tests.Polynomial
{
    /// <summary>
    /// IntegerPolynomial test
    /// </summary>
    public class IntegerPolynomialTest : ITest
    {
        #region Constants
        private const string DESCRIPTION = "Test the validity of the IntegerPolynomial implementation";
        private const string FAILURE = "FAILURE! ";
        private const string SUCCESS = "SUCCESS! IntegerPolynomial tests have executed succesfully.";
        #endregion

        #region Events
        public event EventHandler<TestEventArgs> Progress;
        protected virtual void OnProgress(TestEventArgs e)
        {
            var handler = Progress;
            if (handler != null) handler(this, e);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get: Test Description
        /// </summary>
        public string Description { get { return DESCRIPTION; } }
        #endregion

        #region Public Methods
        /// <summary>
        /// Test the validity of the IntegerPolynomial implementation
        /// </summary>
        /// 
        /// <returns>State</returns>
        public string Test()
        {
            try
            {
                ToBinary4();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial ToBinary"));
                MultTest();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial multiplication"));
                DivTest();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial division"));
                InvertFq();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial InvertFq"));
                InvertF3();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial InvertF3"));
                FromToBinary();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial FromBinary"));
                FromToBinary3Sves();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial FromToBinary3Sves"));
                FromToBinary3Tight();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial FromToBinary3Tight"));
                ResultantMod();
                OnProgress(new TestEventArgs("Passed IntegerPolynomial ResultantMod"));

                return SUCCESS;
            }
            catch (Exception Ex)
            {
                string message = Ex.Message == null ? "" : Ex.Message;
                throw new Exception(FAILURE + message);
            }
        }
        #endregion

        #region Private Methods
        private void ToBinary4()
        {
            IntegerPolynomial a = (IntegerPolynomial)PolynomialGeneratorForTesting.GenerateRandom(743, 2048);
            if (!Compare.AreEqual(a.ToBinary(4), a.ToBinary4()))
                throw new Exception("IntegerPolynomialTest ToBinary4 test failed!");
        }

        private void MultTest()
        {
            // multiplication modulo q
            IntegerPolynomial a = new IntegerPolynomial(new int[] { -1, 1, 1, 0, -1, 0, 1, 0, 0, 1, -1 });
            IntegerPolynomial b = new IntegerPolynomial(new int[] { 14, 11, 26, 24, 14, 16, 30, 7, 25, 6, 19 });
            IntegerPolynomial c = a.Multiply(b, 32);
            AssertEqualsMod(new int[] { 3, -7, -10, -11, 10, 7, 6, 7, 5, -3, -7 }, c.Coeffs, 32);

            a = new IntegerPolynomial(new int[] { 15, 27, 18, 16, 12, 13, 16, 2, 28, 22, 26 });
            b = new IntegerPolynomial(new int[] { -1, 0, 1, 1, 0, 1, 0, 0, -1, 0, -1 });
            c = a.Multiply(b, 32);
            AssertEqualsMod(new int[] { 8, 25, 22, 20, 12, 24, 15, 19, 12, 19, 16 }, c.Coeffs, 32);

            // multiplication without a modulus
            a = new IntegerPolynomial(new int[] { 1, 1, 0, 0, -1, -1, 0, 0, -1, 0, 1 });
            b = new IntegerPolynomial(new int[] { 2, 14, -10, 10, -4, -10, 2, 12, 11, -2, 8 });
            c = a.Multiply(b);

            if (!Compare.AreEqual(new int[] { 0, -13, 15, -12, -26, -39, 2, 17, 13, 17, 26 }, c.Coeffs))
                throw new Exception("IntegerPolynomial multiplication test failed!");

            // mult(p, modulus) should give the same result as mult(p) followed by modulus
            a = new IntegerPolynomial(new int[] { 1, 0, -1, 1, 0, 1, 1, 1, -1, 1, -1 });
            b = new IntegerPolynomial(new int[] { 0, 1, 1, 0, 0, -1, -1, 1, 1, -1, 1 });
            c = a.Multiply(b);
            c.ModPositive(20);
            IntegerPolynomial d = a.Multiply(b, 20);
            d.ModPositive(20);

            if (!Compare.AreEqual(c.Coeffs, d.Coeffs))
                throw new Exception("IntegerPolynomial multiplication test failed!");
        }

        private void DivTest()
        {
            Random rng = new Random();
            for (int i = 0; i < 10; i++)
            {
                IntegerPolynomial poly = (IntegerPolynomial)PolynomialGeneratorForTesting.GenerateRandom(439, 10000);

                // test special case: division by 2048
                IntegerPolynomial a = poly.Clone();
                a.Divide(2048);
                for (int j = 0; j < poly.Coeffs.Length; j++)
                {
                    if (!Compare.Equals((int)Math.Floor((((double)poly.Coeffs[j]) / 2048) + 0.5), a.Coeffs[j]))
                        throw new Exception("IntegerPolynomialTest division test failed!");
                }
                // test the general case
                a = poly.Clone();
                int k = rng.Next(2047) + 1;
                a.Divide(k);
                for (int j = 0; j < poly.Coeffs.Length; j++)
                {
                    if (!Compare.Equals((int)Math.Floor(((double)poly.Coeffs[j] / k) + 0.5), a.Coeffs[j]))
                        throw new Exception("IntegerPolynomialTest division test failed!");
                }

            }
        }

        private void InvertFq()
        {
            // Verify an example from the NTRU tutorial
            IntegerPolynomial a = new IntegerPolynomial(new int[] { -1, 1, 1, 0, -1, 0, 1, 0, 0, 1, -1 });
            IntegerPolynomial b = a.InvertFq(32);
            AssertEqualsMod(new int[] { 5, 9, 6, 16, 4, 15, 16, 22, 20, 18, 30 }, b.Coeffs, 32);
            VerifyInverse(a, b, 32);

            // test 3 random polynomials
            int numInvertible = 0;
            while (numInvertible < 3)
            {
                a = (IntegerPolynomial)PolynomialGeneratorForTesting.generateRandom(853);
                b = a.InvertFq(2048);
                if (b != null)
                {
                    numInvertible++;
                    VerifyInverse(a, b, 2048);
                }
            }

            // test a non-invertible polynomial
            a = new IntegerPolynomial(new int[] { -1, 0, 1, 1, 0, 0, -1, 0, -1, 0, 1 });
            b = a.InvertFq(32);

            if (b != null)
                throw new Exception("IntegerPolynomialTest InvertFq test failed!");
        }

        private void InvertF3()
        {
            IntegerPolynomial a = new IntegerPolynomial(new int[] { -1, 1, 1, 0, -1, 0, 1, 0, 0, 1, -1 });
            IntegerPolynomial b = a.InvertF3();
            AssertEqualsMod(new int[] { 1, 2, 0, 2, 2, 1, 0, 2, 1, 2, 0 }, b.Coeffs, 3);
            VerifyInverse(a, b, 3);

            // test a non-invertible polynomial
            a = new IntegerPolynomial(new int[] { 0, 1, -1, 1, 0, 0, 0, 0, -1, 0, 0 });
            b = a.InvertF3();

            if (b != null)
                throw new Exception("IntegerPolynomialTest InvertF3 test failed!");
        }

        private void FromToBinary()
        {
            byte[] a = ByteUtils.ToBytes(new sbyte[] { -44, -33, 30, -109, 101, -28, -6, -105, -45, 113, -72, 99, 101, 15, 9, 49, -80, -76, 58, 42, -57, -113, -89, -14, -125, 24, 125, -16, 37, -58, 10, -49, -77, -31, 120, 103, -29, 105, -56, -126, -92, 36, 125, 127, -90, 38, 9, 4, 104, 10, -78, -106, -88, -1, -1, -43, -19, 90, 41, 0, -43, 102, 118, -72, -122, 19, -76, 57, -59, -2, 35, 47, 83, 114, 86, -115, -125, 58, 75, 115, -29, -6, 108, 6, -77, -51, 127, -8, -8, -58, -30, -126, 110, -5, -35, -41, -37, 69, 22, -48, 26, 4, -120, -19, -32, -81, -77, 124, -7, -2, -46, -96, 38, -35, 88, 4, -5, 16, 101, 29, 7, 2, 88, 35, -64, 31, -66, -70, 120, -97, 76, -74, -97, -61, 52, -56, 87, -35, 5, 95, -93, -30, 10, 38, 17, -102, -25, 86, 7, -43, 44, -52, -108, 33, -18, -110, -9, -115, 66, -71, 66, 1, -90, -72, 90, -88, -38, 75, 47, -124, -120, -15, -49, -8, 85, 5, 17, -88, 76, 99, -4, 83, 16, -91, 82, 116, 112, -83, 56, -45, -26, 125, 13, -75, -115, 92, -12, -59, 3, -12, 14, -6, 43, -17, 121, 122, 22, 92, -74, 99, -59, -103, 113, 8, -103, 114, 99, -48, 92, -88, 77, 81, 5, 31, -4, -69, -24, 23, 94, 126, 71, 93, 20, 77, 82, -54, -14, 86, 45, -81, 0, 52, -63, -66, 48, 104, -54, 15, -73, -2, -52, 115, 76, 28, -5, -94, -63, 117, -69, 0, 61, 22, -1, 71, -115, 9, -73, -100, -128, -31, 106, -74, -61, -37, 98, -6, 11, -5, 6, -18, -53, -6, 11, -49, 62, 23, 6, -128, 38, -91, 89, -34, 18, -38, -110, -101, 43, 36, 62, 101, 112, 59, -91, 78, -81, 61, 126, -21, -42, -110, -38, -27, 69, 57, 9, 24, -50, -118, 31, -17, 42, 87, -54, 122, -16, 42, -47, -19, -80, 16, 54, -97, -89, 81, -22, -35, 45, 54, -46, 22, -122, -95, -17, 7, -127, 105, -100, -56, -98, -105, 101, -81, 104, 121, -7, 33, 126, 110, -125, -85, 111, -52, 123, -98, 41, -42, 88, -68, -17, 39, -19, -96, -10, -117, 13, -88, -75, -101, -16, -7, 73, 23, -12, 41, -116, -105, -64, -4, 103, 49, -15, -49, 60, 88, -25, -21, 42, 26, 95, -90, -83, -69, 64, -2, 50, -116, -64, 26, -29, -93, -120, -70, 32, -38, 39, -126, -19, 103, 127, 65, 54, 110, 94, 126, -82, -80, -18, 43, 45, 56, -118, 109, 36, -8, 10, 113, 69, 53, -122, -127, 92, -127, -73, 70, -19, -105, -80, -15, -5, 99, -109, -27, 119, -76, -57, -48, 42, -35, 23, 39, -126, 44, -107, -100, -125, 117, -50, 115, -79, -16, 104, 8, -102, 83, -73, 21, -85, 113, -87, -54, 93, 63, -108, -64, 109, -74, 15, 14, -119, -6, -68, 45, 37, -15, -97, -95, -55, 89, 25, -63, -92, -80, -27, -8, 55, 50, 96, -91, 40, -74, 110, -96, 94, 6, 85, 92, 0, 34, -122, 5, -126, 123, 37, -90, -94, 60, 14, 36, 49, -98, -23, 57, 75, 63, 106, -7, -36, -89, 84, 71, 60, -21, 104, -47, 90, -52, -66, 88, -91, -81, -3, 116, 23, 62, -47, -84, -118, 65, 31, 7, -103, 37, -29, 115, -114, 73, 12, -121, 96, -91, -7, 56, 10, -72, 27, -45, 122, -27, -38, 74, 64, 30, -60, 64, -21, 48, 101, 113, 126, -60, -103, 71, 100, -117, 124, -125, 116, 78, 114, -74, 42, -81, -54, 34, 33, -10, 19, 23, 24, 40, 0, -8, 78, 100, 73, -88, -95, -62, -115, -18, 47, 10, -14, -39, 82, 27, -9, -115, -70, 92, -6, 39, 45, -71, -109, -41, 94, -88, -63, 19, -58, -37, -31, 1, 127, -42, 125, -120, -57, 120, -86, -6, 17, -27, -37, 47, 55, -22, -11, -31, 38, -1, 29, 56, -34, -104, -66, -62, 72, -11, -30, -30, 61, -31, 10, -63, 116, -84, 118, -127, 6, 17, -36, 91, 123, 77, 35, 22, 110, 114, 107, -3, 52, 11, 86, 68, -56, 0, 119, -43, -73, 112, 89, -4, -122, -71, -26, 103, -118, -61, -112, -108, -44, -25, -22, 4, 24, 53, -5, -71, 9, -41, 84, -28, 22, 99, 39, -26, -2, -51, 68, 63, -15, 99, 66, -78, 46, -89, 21, -38, -114, -51, 100, -59, 84, -76, -105, 51, 28, 19, 74, 42, 91, -73, 12, -89, -128, 34, 38, -100, 121, -78, 114, -28, 127, -29, 50, 105, -6, 36, 98, -35, 79, -58, 5, -13, -86, -101, -108, -99, -70, 25, 103, 63, 57, 79, -12, -63, 125, -54, 61, 15, 6, -79, 90, 76, 103, -45, 7, 39, 93, 107, 58, 76, 80, 56, -108, 55, -22, 36, 125, -91, -65, 11, 69, 10, -19, -14, -4, -26, -36, 114, 124, 63, -31, 88, 92, 108, 33, -52, -22, 80, -65, 57, 126, 43, -13, 122, -8, 68, 72, 92, -50, 100, -91, 1, -81, 75, 95, -11, -99, 38, 121, -20, -70, 82, -125, -94, -18, 16, 59, 89, 18, -96, 91, -97, 62, -96, 127, 45, 70, 16, 84, -43, -75, -118, 81, 58, 84, -115, -120, -3, 41, -103, -70, 123, 26, 101, 33, 58, 13, -11, -73, -84, -47, -7, 81, -63, 60, -45, 30, 100, -51, -15, 73, 58, -119, -3, 62, -63, -17, -69, -44, 60, -54, -115, -59, 23, -59, 98, -89, -72, 20, -96, 27, 53, -89, 59, -85, -29, 120, 23, 62, 8, -86, 113, 87, -15, 102, 106, -104, 57, -57, 37, 110, 118, 109, 25, 64, 26, -20, -86, -2, 60, -70, -33, 67, 13, -28, -29, -63, -37, 67, 99, 84, 121, -126, -38, 45, 24, 122, 51, 11, -19, -80, 26, -106, -95, 82, 69, -2, -75, 62, 106, -120, 87, -107, 87, 17, 102, -52, -16, 22, 12, -86, -48, -95, -61, 109, 64, -29, 111, 40, -90, -35, 49, 88, -15, 122, 127, 87, 113, 116, 93, 100, 28, -70, -87, -40, -1, -126, -114, 7, 79, 16, 2, -47, -98, -102, 49, 58, 61, -32, 44, 18, -26, 37, 27, -123, -76, 56, 91, 51, -21, -48, -122, -33, 40, -8, -62, -56, -126, 91, -51, 76, -29, 127, -22, -18, -110, 27, 13, -111, 81, 51, -104, 70, 98, 12, 120, -7, 15, 104, -43, -104, 124, 46, 116, 7, -26, 21, 33, 105, 17, -99, -42, -106, 8, -85, 39, 8, 79, -54, -81, 109, 40, 25, 29, -18, -90, 22, 85, -12, -16, 61, 49, -31, 127, 64, 5, 25, 39, -65, -42, 13, -97, -92, 36, -126, -18, -4, -22, -14, 109, -93, -76, -5, 13, 74, 44, 103, 79, 110, 85, 58, 39, -24, 119, 120, 122, 120, 43, 110, 67, 21, 47, 39, -48, 7, 91, -51, 126, 100, -38, -124, 0, -97, 99, -123, 118, -27, 8, 102, -106, -23, -53, -4, -56, -9, -126, -85, 93, -4, -5, 4, 49, 29, 2, 63, 78, -32, -106, 118, 111, 52, 54, 74, 53, 106, 39, -95, -38, -18, 118, -5, 94, -83, -97, -27, 62, -56, -90, -36, 43, 43, -113, 119, -89, 44, -108, -46, 66, 28, 66, -38, 3, -62, -83, -35, -127, -2, 51, 104, 105, 40, 76, -10, -124, -95, 52, 11, 101, -32, -122, -73, -17, 37, -126, 68, -126, 55, 112, -126, 38, 99, -63, 123, -74, -31, 58, 8, 93, -68, 111, -22, -24, -23, 9, -87, -25, -115, 81, -116, -91, 60, 96, -102, -1, -7, 73, 99, 46, -78, 62, 48, -116, -52, -44, -5, 82, -45, 5, -55, -101, 101, 65, -109, -108, 26, 98, -55, 11, -86, 57, 30, 92, -58, 20, 82, 65, 103, 27, -64, 76, 123, -56, -16, -111, -83, 125, 65, 111, 9, 123, 14, 119, 126, -80, 79, 94, -19, 66, -25, 35, 112, -64, 10, -66, -86, 51, 56, -78, 103, 92, -116, 8, 75, 41, -49, -79, -53, 125, -32, -76, -27, 59, -8, -4, -94, -104, -15, 79, -7, -124, 32, -87, -104, 85, -118, -36, 125, 65, 111, -105, 5, -105, 40, -50, 2, 118, 123, -54, 59, -22, 94, 20, 99, -87, -27, 28, -30, -109, 72, -19, 92, 60, 19, 115, 47, 96, -96, 10, -74, 60, 96, -86, 101, 101, 68, -44, -72, 9, -36, 126, 96, -45, -12, 9, 14, -15, 79, -79, -48, 8, -107, -81, 47, 35, -36, -107, -120, -36, -124, 37, 103, -60, -35, -74, 100, -38, -88, -99, -99, -94, -107, 79, 115, 108, 54, 119, 73, 84, 110, -74, 92, 57, 108, 80, 47, -36, -119, -115, 58, -62, -4, -97, 43, -98, 5, 112, 47, 59, -89, 82, -69, -103, 39, -29, 75, -9, -94, -72, 99, -64, 22, -10, 21, 89, 101, 21, 94, -30, -17, 73, -36, -68, -89, -91, -94, 99, -106, 119, -116, 123, -19, 54, -99, 64, -119, 82, 120, -106, -99, 80, 69, 29, -48, 77, 28, 13, 92, -107, -77, 94, -116, 108, 89, -115, 96, -41, 25, 99, -65, 118, -5, -16, 48, -122, 5, 50, -123, -115, 13, 24, 7, 15, -103, -62, -71, 92, -82, -5, -70, 49, -6, -51, -17, -47, 12, 46, -86, 30, 93, 84, -101, 43, -92, -87, -118, -110, -32, 52, 115, -4, 36, -2, -79, -69, -46, -110, 70, -82, 6, 21, -27, -11, 94, 42, -81, -96, 116, -102, -38, 36, 32, 91, 28, 80, -45, 116, -94, -33, -5, -102, 64, -96, 27, -2, 100, -126, 59, -71, 33, -36, -124, 123, 99, -76, 108, 127, -11, -24, -19, 84, -6, 19, 105, -19, -18, 120, -14, 23, 39, 54, 87, 105, 58, -95, -15, 127, -65, 114, 49, 4, -66, 32, -7, 84, 43, -103, 76, 11, 36, -68, -3, -98, -5, -43, 35, -48, 20, -40, -33, -123, 1, -54, -44, 99, -68, 8, -100, 97, -49, -10, 110, 49, 84, 46, -85, 98, -103, -58, -4, 104, -100, -40, -79, 67, -20, -95, 85, 51, 73, 10, -25, 102, 68, -97, -83, -39, 35, 2, -111, 71, 62, -89, 20, 25, -126, 17, -81, -29, 39, -27, -55, 55, -122, 97, 23, -99, 55, 86, 33, -9, 8, 55, -40, -84, 39, 38, 37, -29, 87, 113, -118, -26, 123, -95, 24, -126, 119, -94, 17, 83, -43, 10, 63, -98, 72, 8, 16, -95, -96, 119, -91, 6, 71, -60, 1, -77, 4, 53, -121, 55, 7, 36, -86, -49, -118, -121, 56, 84, -49, -57, -99, 3, -68, 37, -108, -72, 114, -74, 120, 3, 121, -28, -106, 54, -20, 63, -121, -85, -59, -111, 32, 13, -69, 122, 90, 5, 40, 88, 15, -90, 125, -28, 89, 95, 73, 96, 60, -60, -51, 102, 7, 57, 91, 59, 15, 92, -76, -34, -23, -77, 90, 45, 91, 77, -63, 94, -127, 74, -97, -44, 50, -87, -94, -25, -71, 112, 127, -117, 6, 32, -113, 54, 83, -31, 111, -73, 53, 34, -32, -98, 125, -39, 63, 15, 72, -69, 87, -118, 108, 17, 84, 15, 61, -47, 54, -24, -79, 91, 28, -28, 66, 53, 22, 9, -28, -12, 38, 64, 75, -122, 96, -59, -45, 4, -19, 47, -30, 75, -94, 62, -64, 76, -49, 19, -66, -34, 3, 84, -2, -54, 13, -84, 86, -117, 94, -27, 89, 16, 96, 52, -77, -36, -116, 27, -52, -33, -50, 14, -59, 77, 93, -109, 8, -89, 81, -114, -29, -94, 73, -119, -56, -19, 88, -17, -33, 125, -18, -68, 113, 40, -128, -112, -119, -106, -106, -30, 23, -77, 49, 3, 98, -101, 99, -107, -121, -12, -112, 24, -74, -74, 79, -17, 96, 65, -52, 86, -63, 45, 84, 119, -42, 61, -91, 29, -87, 65, -85, 99, -14, 71, 33, -41, -48, -2, -121, 78, -38, 41, -7, -37, 48, 122, 61, -124, 42, -22, 24, 2, -49, 74, -81, -88, -89, -107, 109, 53, -68, 90, -117, 123, -109, -28, 12, 80, 120, 26, -104, 73, 70, -36, 34, -80, -104, 23, 16, 14, -96, -5, 27, 71, 25, -8, -125, 58, 88, -52, -97, -97, -93, 11, -44, 116, 42, -102, -100, -31, -86, 71, 84, 70, 27, 117, -67, 92, -84, -13, 54, -102, 34, 5, 19, -76, 71, 89, 22, -49, -34, -29 });
            IntegerPolynomial poly = IntegerPolynomial.FromBinary(a, 1499, 2048);
            byte[] b = poly.ToBinary(2048);

            //Verify that bytes 0..2047 match, ignore non-relevant bits of byte 2048
            if (!Compare.AreEqual(a.CopyOf(2047), b.CopyOf(2047)))
                throw new Exception("IntegerPolynomialTest FromToBinary test failed!");
            if (!Compare.Equals((a[a.Length - 1] & 1) >> (7 - (1499 * 11) % 8), (b[b.Length - 1] & 1) >> (7 - (1499 * 11) % 8)))
                throw new Exception("IntegerPolynomialTest FromToBinary test failed!");
        }

        private void FromToBinary3Sves()
        {
             byte[] a = ByteUtils.ToBytes(new sbyte[] { -112, -78, 19, 15, 99, -65, -56, -90, 44, -93, -109, 104, 40, 90, -84, -21, -124, 51, -33, 4, -51, -106, 33, 86, -76, 42, 41, -17, 47, 79, 81, -29, 15, 116, 101, 120, 116, 32, 116, 111, 32, 101, 110, 99, 114, 121, 112, 116, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
             IntegerPolynomial poly = IntegerPolynomial.FromBinary3Sves(a, 1499, false);
             byte[] b = poly.ToBinary3Sves(false);

             if (!Compare.AreEqual(a, b))
                 throw new Exception("IntegerPolynomialTest FromToBinary3Sves test failed!");
        }

        private void FromToBinary3Tight()
        {
            int[] c = new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, -1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 1, 0, 1, 0, -1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, -1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0 };
            IntegerPolynomial poly1 = new IntegerPolynomial(c);
            IntegerPolynomial poly2 = IntegerPolynomial.FromBinary3Tight(poly1.ToBinary3Tight(), c.Length);

            if (!Compare.AreEqual(poly1.Coeffs, poly2.Coeffs))
                throw new Exception("IntegerPolynomialTest FromToBinary3Tight test failed!");

            IntegerPolynomial poly3 = new IntegerPolynomial(new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, -1, -1, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, -1, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, -1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 });
            byte[] arr = poly3.ToBinary3Tight();
            IntegerPolynomial poly4 = IntegerPolynomial.FromBinary3Tight(arr, 1499);

            if (!Compare.AreEqual(poly3.Coeffs, poly4.Coeffs))
                throw new Exception("IntegerPolynomialTest FromToBinary3Tight test failed!");

            IntegerPolynomial poly5 = new IntegerPolynomial(new int[] { 0, 0, 0, 1, -1, -1, -1 });
            arr = poly5.ToBinary3Tight();
            IntegerPolynomial poly6 = IntegerPolynomial.FromBinary3Tight(arr, 7);

            if (!Compare.AreEqual(poly5.Coeffs, poly6.Coeffs))
                throw new Exception("IntegerPolynomialTest FromToBinary3Tight test failed!");

            for (int i = 0; i < 100; i++)
            {
                IntegerPolynomial poly7 = (IntegerPolynomial)PolynomialGeneratorForTesting.generateRandom(157);
                arr = poly7.ToBinary3Tight();
                IntegerPolynomial poly8 = IntegerPolynomial.FromBinary3Tight(arr, 157);

                if (!Compare.AreEqual(poly7.Coeffs, poly8.Coeffs))
                    throw new Exception("IntegerPolynomialTest FromToBinary3Tight test failed!");
            }
        }

        private void ResultantMod()
        {
            int p = 46337;   // prime; must be less than sqrt(2^31) or integer overflows will occur

            IntegerPolynomial a = new IntegerPolynomial(new int[] { 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 1, -1, 0, 0, -1, 0, 0, 0, 1, 0, 0, 0, -1, -1, 0, -1, 1, -1, 0, -1, 0, -1, -1, -1, 0, 0, 0, 1, 1, -1, -1, -1, 0, -1, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 1, 1, -1, 0, 1, -1, 0, 1, 0, 1, 0, -1, -1, 0, 1, 0, -1, 1, 1, 1, 1, 0, 0, -1, -1, 1, 0, 0, -1, -1, 0, -1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, -1, 0, 0, 0, 0, -1, 0, 0, 0, 1, 0, 1, 0, 1, -1, 0, 0, 1, 1, 1, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 1, 0, -1, -1, 0, -1, -1, -1, 0, -1, -1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1, -1, 0, 1, 0, -1, 0, 0, 0, 0, 0, 0, -1, -1, 0, -1, -1, 1, 1, 0, 0, -1, 1, 0, 0, 0, -1, 1, -1, 0, -1, 0, 0, 0, -1, 0, 0, 0, 0, 0, -1, 1, 1, 0, 0, -1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, -1, 0, 1, 0, -1, -1, 0, 0, 0, 0, 0, 1, -1, 0, 0, 0, 1, -1, 1, -1, -1, 1, -1, 0, 1, 0, 0, 0, 1, 0, 0, 1, -1, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, -1, 0, 1, -1, 0, 0, 1, 1, 0, 0, 1, 1, 0, -1, 0, -1, 1, -1, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, -1, 0, 0, 1, -1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, 0, -1, -1, 0, 0, -1, 0, 1, 1, -1, 1, -1, 0, 0, 0, 1 });
            VerifyResultant(a, a.Resultant(p), p);

            for (int i = 0; i < 10; i++)
            {
                a = (IntegerPolynomial)PolynomialGeneratorForTesting.generateRandom(853);
                VerifyResultant(a, a.Resultant(p), p);
            }
        }

        private void AssertEqualsMod(int[] arr1, int[] arr2, int m)
        {
            if (!Compare.Equals(arr1.Length, arr2.Length))
                throw new Exception("IntegerPolynomialTest Mod test failed!");

            for (int i = 0; i < arr1.Length; i++)
            {
                if (!Compare.Equals((arr1[i] + m) % m, (arr2[i] + m) % m))
                    throw new Exception("IntegerPolynomialTest Mod test failed!");
            }
        }

        // tests if a*b=1 (mod modulus)
        private void VerifyInverse(IntegerPolynomial a, IntegerPolynomial b, int modulus)
        {
            IntegerPolynomial c = a.Multiply(b, modulus);
            for (int i = 1; i < c.Coeffs.Length; i++)
                c.Coeffs[i] %= modulus;

            c.EnsurePositive(modulus);
            if (!c.EqualsOne())
                throw new Exception("IntegerPolynomialTest Modulus inverse test failed!");
        }

        // verifies that res=rho*a mod x^n-1
        private void VerifyResultant(IntegerPolynomial a, Resultant r)
        {
            BigIntPolynomial b = new BigIntPolynomial(a).MultSmall(r.Rho);

            for (int j = 1; j < b.Coeffs.Length - 1; j++)
            {
                if (!Compare.Equals(BigInteger.Zero, b.Coeffs[j]))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }
            if (r.Res.Equals(BigInteger.Zero))
            {
                if (!Compare.Equals(BigInteger.Zero, b.Coeffs[0].Subtract(b.Coeffs[b.Coeffs.Length - 1])))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }
            else
            {
                if (!Compare.Equals(BigInteger.Zero, (b.Coeffs[0].Subtract(b.Coeffs[b.Coeffs.Length - 1]).Mod(r.Res))))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }

            if (!Compare.Equals(b.Coeffs[0].Subtract(r.Res), b.Coeffs[b.Coeffs.Length - 1].Negate()))
                throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
        }

        // verifies that res=rho*a mod x^n-1 mod p
        private void VerifyResultant(IntegerPolynomial a, Resultant r, int p)
        {
            BigIntPolynomial b = new BigIntPolynomial(a).MultSmall(r.Rho);
            b.Mod(BigInteger.ValueOf(p));

            for (int j = 1; j < b.Coeffs.Length - 1; j++)
            {
                if (!Compare.Equals(BigInteger.Zero, b.Coeffs[j]))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }
            if (r.Res.Equals(BigInteger.Zero))
            {
                if (!Compare.Equals(BigInteger.Zero, b.Coeffs[0].Subtract(b.Coeffs[b.Coeffs.Length - 1])))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }
            else
            {
                if (!Compare.Equals(BigInteger.Zero, (b.Coeffs[0].Subtract(b.Coeffs[b.Coeffs.Length - 1]).Subtract(r.Res).Mod(BigInteger.ValueOf(p)))))
                    throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
            }

            if (!Compare.Equals(BigInteger.Zero, b.Coeffs[0].Subtract(r.Res).Subtract(b.Coeffs[b.Coeffs.Length - 1].Negate()).Mod(BigInteger.ValueOf(p))))
                throw new Exception("IntegerPolynomialTest VerifyResultant test failed!");
        }

        private void AddTest()
        {
            NtruParameters param = DefinedParameters.EES1087EP2;
            IntegerPolynomial a = PolynomialGeneratorForTesting.GenerateRandom(param.N, param.Q);
            ITernaryPolynomial b = PolynomialGeneratorForTesting.generateRandom(1087);

            IntegerPolynomial c1 = a.Clone();
            c1.Add(b.ToIntegerPolynomial());

            IntegerPolynomial c2 = a.Clone();
            c2.Add(b);

            if (!Compare.Equals(c1, c2))
                throw new Exception("IntegerPolynomialTest addition test failed!");
        }
        #endregion
    }
}