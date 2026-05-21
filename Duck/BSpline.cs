using System.Numerics;

namespace Duck;

/// <summary>
///     B-spline curve C(t) = \sum P_i \cdot N_i^k(t) .
///     External parameter t \in [0, 1] is mapped to the valid knot span internally.
/// </summary>
public sealed class BSpline
{
    private readonly bool _closed;
    private readonly Vector3[] _controlPoints;
    private readonly int _degree;
    private readonly float[] _knots;

    /// <inheritdoc cref="_tMin" />
    private readonly float _tMax;

    /// Internal t range for the valid segment of the knot vector
    private readonly float _tMin;

    /// <summary>
    ///     Open: clamped knot vector; curve interpolates first and last control points.
    ///     Closed: first <paramref name="degree" /> points are appended to <paramref name="controlPoints" />;
    ///     uniform knot vector;
    ///     valid range [degree, n + degree) gives C(0) = C(1).
    /// </summary>
    public BSpline(Vector3[] controlPoints, int degree = 3, bool closed = false)
    {
        _degree = degree;
        _closed = closed;

        if (closed)
        {
            // wrap first [degree] control points at the end to close the curve
            _controlPoints = new Vector3[controlPoints.Length + degree];
            controlPoints.CopyTo(_controlPoints, 0);
            for (int i = 0; i < degree; i++)
            {
                _controlPoints[controlPoints.Length + i] = controlPoints[i];
            }

            _knots = BuildUniformKnots(_controlPoints.Length, degree);
            _tMin = _knots[degree];
            _tMax = _knots[controlPoints.Length + degree];
        }
        else
        {
            _controlPoints = controlPoints;
            _knots = BuildClampedKnots(controlPoints.Length, degree);
            _tMin = _knots[degree];
            _tMax = _knots[controlPoints.Length];
        }
    }

    /// <summary>
    ///     Evaluate curve position at <paramref name="t" /> in [0, 1].
    /// </summary>
    public Vector3 Evaluate(float t)
    {
        float ti = ToInternal(t);
        int span = FindSpan(ti);
        float[] basis = BasisFunctions(span, ti);

        Vector3 result = Vector3.Zero;
        for (int k = 0; k <= _degree; k++)
        {
            result += basis[k] * _controlPoints[span - _degree + k];
        }

        return result;
    }

    /// <summary>
    ///     Approximate tangent at <paramref name="t" /> via differences.
    /// </summary>
    public Vector3 Tangent(float t)
    {
        const float eps = 0.001f;
        float t0 = Math.Clamp(t - eps, 0f, 1f);
        float t1 = Math.Clamp(t + eps, 0f, 1f);

        if (!_closed)
        {
            return Vector3.Normalize(Evaluate(t1) - Evaluate(t0));
        }

        // For closed curves wrap around
        t0 = (t - eps + 1f) % 1f;
        t1 = (t + eps) % 1f;
        return Vector3.Normalize(Evaluate(t1) - Evaluate(t0));
    }

    /// <summary>
    ///     Returns the degree + 1 non-zero basis function values
    ///     N[<paramref name="span" /> - degree...<paramref name="span" />] at internal knot value <paramref name="t" />.
    /// </summary>
    private float[] BasisFunctions(int span, float t)
    {
        float[] basis = new float[_degree + 1];
        float[] a = new float[_degree + 1];
        float[] b = new float[_degree + 1];

        basis[0] = 1f;
        for (int j = 1; j <= _degree; j++)
        {
            a[j] = _knots[span + j] - t;
            b[j] = t - _knots[span + 1 - j];
            float saved = 0f;
            for (int k = 1; k <= j; k++)
            {
                float term = basis[k - 1] / (a[k] + b[j + 1 - k]);
                basis[k - 1] = saved + (a[k] * term);
                saved = b[j + 1 - k] * term;
            }

            basis[j] = saved;
        }

        return basis;
    }

    /// <summary>
    ///     Clamped knot vector: <paramref name="d" /> + 1 zeros,
    ///     interior integers 1...<paramref name="n" /> - <paramref name="d" /> - 1,
    ///     <paramref name="d" /> + 1 copies of <paramref name="n" /> - <paramref name="d" />.
    ///     Length = <paramref name="n" /> + <paramref name="d" /> + 1.
    /// </summary>
    private static float[] BuildClampedKnots(int n, int d)
    {
        int m = n + d + 1;
        float[] knots = new float[m];
        for (int i = 0; i < m; i++)
        {
            if (i <= d)
            {
                knots[i] = 0f;
            }
            else if (i >= n)
            {
                knots[i] = n - d;
            }
            else
            {
                knots[i] = i - d;
            }
        }

        return knots;
    }

    /// <summary>Uniform knot vector: knots[i] = i. Length = <paramref name="n" /> + <paramref name="d" /> + 1.</summary>
    private static float[] BuildUniformKnots(int n, int d)
    {
        int m = n + d + 1;
        float[] knots = new float[m];
        for (int i = 0; i < m; i++)
        {
            knots[i] = i;
        }

        return knots;
    }

    /// <summary>
    ///     Find knot span index such that knots[span] &lt;= <paramref name="t" /> &lt; knots[span + 1]
    /// </summary>
    private int FindSpan(float t)
    {
        int n = _controlPoints.Length;
        int low = _degree;
        int high = n;
        int mid = (low + high) / 2;
        while (t < _knots[mid] || t >= _knots[mid + 1])
        {
            if (t < _knots[mid])
            {
                high = mid;
            }
            else
            {
                low = mid;
            }

            mid = (low + high) / 2;
        }

        return mid;
    }

    /// <summary>
    ///     Maps t \in [0, 1] → [<see cref="_tMin" />, <see cref="_tMax" />);
    ///     clamps just below <see cref="_tMax" /> to keep <see cref="FindSpan" /> in bounds.
    /// </summary>
    private float ToInternal(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float ti = _tMin + (t * (_tMax - _tMin));
        if (ti >= _tMax)
        {
            ti = _tMax - 1e-6f;
        }

        return ti;
    }
}