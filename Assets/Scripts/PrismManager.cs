using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;
	
	public KdTree<Prism> prisms = new KdTree<Prism>();
    //private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism,bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
					prisms.UpdatePositions();
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions
	KdTree<Prism> newobj = new KdTree<Prism>();
    private IEnumerable<PrismCollision> PotentialCollisions()
    {	
	
	for (int i = 0 ; i < prisms.Count ; i++){
				newobj.Clear();
				//newobj.AddAll(prisms.Where((v, k) => k >= prisms[i]).ToList());
				var x = prisms.FindClosest(prisms[i].transform.position);
				print(x);
				print(prisms[i]);
				if(x.Equals(prisms[i])==false){
				var setPrim = new PrismCollision{ a = prisms[i] , b = x}; 
				Debug.DrawLine(prisms[i].transform.position, x.transform.position, Color.green);

			yield return setPrim;
			}
		
		}
		

        yield break;
    }

    private bool CheckCollision(PrismCollision collision){
        var prismA = collision.a;
        var prismB = collision.b;
        List<Vector2> Simplex = new List<Vector2>();
        int sizeOfList = Simplex.Count;

        collision.penetrationDepthVectorAB = Vector3.zero;
        return true;

        Vector2 getFarthestPointInDirection(Vector2 d) {
            int index = 0;
            double maxDot = (int)Vector2.Dot(Simplex[index], d);
            if (Simplex != null)
            {
                for (int i = 1; i < sizeOfList; i++)
                {
                    int dot = (int)Vector2.Dot(Simplex[i], d);
                    if (dot > maxDot)
                    {
                        maxDot = dot;
                        index = i;
                    }
                }
            }
            return Simplex[index];
        }
        Vector2 support(Prism a, Prism b, Vector2 d){
            // get points on the edge of the shapes in opposite directions
            Vector2 p1 = a.getFarthestPointInDirection(d);
            Vector2 p2 = b.getFarthestPointInDirection(new Vector2(-d.x, -d.y));

            // Minkowski Difference
            Vector2 p3 = new Vector2((p1 - p2).x, (p1 - p2).y);

            // p3 is now a point in Minkowski space on the edge of the Minkowski Difference
            return p3;
        }

        bool containsOrigin(ref Vector2 d){
            Vector2 a = Simplex.Last();
            Vector2 ao = new Vector2(-a.x, -a.y);
            if (Simplex.Count() == 3){
                support(prismA, prismB, d);
                if((a.x == 0) && (a.y == 0) {
                    return true;
                }
            }
            return false;

        }
            bool IsCollide(Prism a, Prism b) {
            Vector2 last = Simplex[sizeOfList - 1];
            Vector2 dark = new Vector2(1, -1);
            Simplex.Add(dark);
            // negate d for the next point
            Negate(dark);
            // start looping
            while (true)
            {
                // add a new point to the simplex because we haven't terminated yet
                Simplex.Add(support(a, b, dark));
                // make sure that the last point we added actually passed the origin
                int dot6 = (int)Vector2.Dot(last, dark);
                if (dot6 <= 0)
                {
                    // if the point added last was not past the origin in the direction of d
                    // then the Minkowski Sum cannot possibly contain the origin since
                    // the last point added is on the edge of the Minkowski Difference
                    return false;
                }
                else
                {
                    if (containsOrigin(ref dark))
                    {
                        // if it does then we know there is a collision
                        return true;
                    }
                }
            }
        }


    }

    #endregion

    #region Private Functions

    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        prismObjA.transform.position += pushA;
        prismObjB.transform.position += pushB;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }
    
    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();
        
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
